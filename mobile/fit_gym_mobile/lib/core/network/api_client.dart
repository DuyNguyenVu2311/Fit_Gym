import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../features/member/member_models.dart';
import '../../features/member/renewal_models.dart';
import '../../features/organization/organization_brand.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient({http.Client? client}) : _client = client ?? http.Client();

  static const String baseUrl = String.fromEnvironment(
    'FITGYM_API_URL',
    defaultValue: 'http://10.0.2.2:5202',
  );

  final http.Client _client;
  String? _accessToken;

  Future<OrganizationBrand> loadBrand() async {
    final response = await _client
        .get(
          Uri.parse('$baseUrl/api/organization'),
          headers: {'Accept': 'application/json'},
        )
        .timeout(const Duration(seconds: 8));
    if (response.statusCode != 200) {
      throw ApiException(
        'Không thể tải thông tin đơn vị.',
        statusCode: response.statusCode,
      );
    }
    return OrganizationBrand.fromJson(
      jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>,
    );
  }

  Future<void> login(String userNameOrEmail, String password) async {
    final response = await _client.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'userNameOrEmail': userNameOrEmail,
        'password': password,
      }),
    );
    if (response.statusCode != 200) {
      throw ApiException(
        'Tài khoản hoặc mật khẩu không đúng.',
        statusCode: response.statusCode,
      );
    }
    final json = jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>;
    _accessToken = json['accessToken'] as String?;
    if (_accessToken == null || _accessToken!.isEmpty) {
      throw const ApiException('Máy chủ không trả về access token.');
    }
  }

  void logout() => _accessToken = null;

  Future<MemberDashboardData> loadDashboard() async {
    final results = await Future.wait<dynamic>([
      _get('/api/member/me'),
      _get('/api/member/subscriptions/current', allowNoContent: true),
      _get('/api/member/payments'),
      _get('/api/member/check-ins'),
      _get('/api/member/subscriptions'),
    ]);
    return MemberDashboardData(
      profile: MemberProfile.fromJson(results[0] as Map<String, dynamic>),
      subscription: results[1] == null
          ? null
          : CurrentSubscription.fromJson(results[1] as Map<String, dynamic>),
      payments: (results[2] as List<dynamic>)
          .map((item) => MemberPayment.fromJson(item as Map<String, dynamic>))
          .toList(),
      checkIns: (results[3] as List<dynamic>)
          .map((item) => MemberCheckIn.fromJson(item as Map<String, dynamic>))
          .toList(),
      subscriptions: (results[4] as List<dynamic>)
          .map((item) => MemberRegistration.fromJson(item as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<MemberRegistration>> loadSubscriptions() async {
    final rows = await _get('/api/member/subscriptions') as List<dynamic>;
    return rows.map((row) => MemberRegistration.fromJson(row as Map<String, dynamic>)).toList();
  }

  Future<List<MemberPackage>> loadPackages() async {
    final rows = await _get('/api/member/packages') as List<dynamic>;
    return rows.map((row) => MemberPackage.fromJson(row as Map<String, dynamic>)).toList();
  }

  Future<RenewalQuote> quoteRenewal(int registrationId, int packageId) async {
    final uri = Uri(path: '/api/member/renewals/quote', queryParameters: {
      'registrationId': '$registrationId',
      'packageId': '$packageId',
    });
    return RenewalQuote.fromJson(await _get(uri.toString()) as Map<String, dynamic>);
  }

  Future<RenewalResult> createRenewal(int registrationId, int packageId,
      RenewalQuote quote, String requestKey) async {
    final result = await _post('/api/member/renewals', {
      'sourceRegistrationId': registrationId,
      'packageId': packageId,
      'expectedStartDate': quote.startDate.toIso8601String(),
      'expectedEndDate': quote.endDate.toIso8601String(),
      'expectedTotalAmount': quote.totalAmount,
      'requestKey': requestKey,
    });
    return RenewalResult.fromJson(result as Map<String, dynamic>);
  }

  Future<List<int>> loadPaymentQr(int paymentId) async {
    final response = await _client.get(
      Uri.parse('$baseUrl/api/member/payments/$paymentId/qr'),
      headers: _authorizedHeaders(),
    );
    _ensureSuccess(response);
    if (response.headers['content-type']?.startsWith('image/png') != true) {
      throw const ApiException('Không thể tải mã QR thanh toán.');
    }
    return response.bodyBytes;
  }

  Future<dynamic> _post(String path, Map<String, dynamic> body) async {
    final response = await _client.post(Uri.parse('$baseUrl$path'),
      headers: {..._authorizedHeaders(), 'Content-Type': 'application/json'},
      body: jsonEncode(body),
    );
    _ensureSuccess(response);
    return jsonDecode(utf8.decode(response.bodyBytes));
  }

  Map<String, String> _authorizedHeaders() {
    final token = _accessToken;
    if (token == null) {
      throw const ApiException('Phiên đăng nhập đã kết thúc.', statusCode: 401);
    }
    return {'Authorization': 'Bearer $token', 'Accept': 'application/json'};
  }

  void _ensureSuccess(http.Response response) {
    if (response.statusCode == 401) {
      throw const ApiException('Phiên đăng nhập đã hết hạn.', statusCode: 401);
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      String? message;
      try {
        final body = jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>;
        message = body['message'] as String?;
      } catch (_) { /* The server may return a non-JSON error. */ }
      throw ApiException(message ?? 'Không thể xử lý yêu cầu.', statusCode: response.statusCode);
    }
  }

  Future<dynamic> _get(String path, {bool allowNoContent = false}) async {
    final token = _accessToken;
    if (token == null) {
      throw const ApiException('Phiên đăng nhập đã kết thúc.', statusCode: 401);
    }
    final response = await _client.get(
      Uri.parse('$baseUrl$path'),
      headers: {'Authorization': 'Bearer $token', 'Accept': 'application/json'},
    );
    if (allowNoContent && response.statusCode == 204) return null;
    if (response.statusCode == 401) {
      throw const ApiException('Phiên đăng nhập đã hết hạn.', statusCode: 401);
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(
        'Không thể tải dữ liệu từ máy chủ.',
        statusCode: response.statusCode,
      );
    }
    return jsonDecode(utf8.decode(response.bodyBytes));
  }
}
