import 'dart:async';
import 'dart:convert';

import 'package:fit_gym_mobile/core/network/api_client.dart';
import 'package:fit_gym_mobile/features/member/renewal_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test('renewal keeps the server quote and one request key', () async {
    final requests = <http.Request>[];
    final client = ApiClient(client: MockClient((request) async {
      requests.add(request);
      if (request.url.path == '/api/auth/login') {
        return http.Response('{"accessToken":"member-token"}', 200);
      }
      expect(request.headers['Authorization'], 'Bearer member-token');
      if (request.url.path == '/api/member/subscriptions') {
        return http.Response.bytes(utf8.encode(jsonEncode([
          {
            'registrationId': 4,
            'sourceRegistrationId': null,
            'packageId': 2,
            'packageName': 'Gói 1 tháng',
            'startDate': '2026-11-01T00:00:00',
            'endDate': '2026-11-30T00:00:00',
            'totalAmount': 300000,
            'registrationStatus': 'HieuLuc',
            'paymentStatus': 'ThanhCong',
            'validity': 'Usable'
          }
        ])), 200, headers: {'content-type': 'application/json; charset=utf-8'});
      }
      if (request.url.path == '/api/member/packages') {
        return http.Response.bytes(utf8.encode('[{"packageId":2,"name":"Gói 1 tháng","durationMonths":1,"price":300000}]'),
            200, headers: {'content-type': 'application/json; charset=utf-8'});
      }
      if (request.url.path == '/api/member/renewals/quote') {
        expect(request.url.queryParameters['registrationId'], '4');
        expect(request.url.queryParameters['packageId'], '2');
        return http.Response('{"startDate":"2026-12-01T00:00:00","endDate":"2026-12-31T00:00:00","totalAmount":300000}', 200);
      }
      if (request.url.path == '/api/member/renewals') {
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['sourceRegistrationId'], 4);
        expect(body['packageId'], 2);
        expect(body['expectedTotalAmount'], 300000);
        expect((body['requestKey'] as String).isNotEmpty, isTrue);
        return http.Response('{"registrationId":5,"paymentId":8,"paymentRequestCode":"TT-8"}', 200);
      }
      if (request.url.path == '/api/member/payments/8/qr') {
        return http.Response.bytes([137, 80, 78, 71], 200,
            headers: {'content-type': 'image/png'});
      }
      return http.Response('not found', 404);
    }));

    await client.login('member', 'password');
    expect((await client.loadSubscriptions()).single.isUsable, isTrue);
    expect((await client.loadPackages()).single.price, 300000);
    final quote = await client.quoteRenewal(4, 2);
    final result = await client.createRenewal(4, 2, quote, '0123456789abcdef0123456789abcdef');
    expect(result.paymentId, 8);
    expect(await client.loadPaymentQr(8), [137, 80, 78, 71]);
    expect(requests.where((r) => r.url.path == '/api/member/renewals').length, 1);
  });

  testWidgets('member sees the server quote before creating a pending renewal', (tester) async {
    final qrResponse = Completer<http.Response>();
    final client = ApiClient(client: MockClient((request) async {
      switch (request.url.path) {
        case '/api/auth/login':
          return http.Response('{"accessToken":"member-token"}', 200);
        case '/api/member/subscriptions':
          return http.Response.bytes(utf8.encode('[{"registrationId":4,"packageName":"Gói 1 tháng","startDate":"2026-11-01T00:00:00","endDate":"2026-11-30T00:00:00","registrationStatus":"HieuLuc","paymentStatus":"ThanhCong","validity":"Usable"}]'), 200);
        case '/api/member/packages':
          return http.Response.bytes(utf8.encode('[{"packageId":2,"name":"Gói 1 tháng","durationMonths":1,"price":300000}]'), 200);
        case '/api/member/renewals/quote':
          return http.Response('{"startDate":"2026-12-01T00:00:00","endDate":"2026-12-31T00:00:00","totalAmount":300000}', 200);
        case '/api/member/renewals':
          final body = jsonDecode(request.body) as Map<String, dynamic>;
          expect((body['requestKey'] as String).length, 32);
          return http.Response('{"registrationId":5,"paymentId":8,"paymentRequestCode":"TT-8"}', 200);
        case '/api/member/payments/8/qr':
          return qrResponse.future;
      }
      return http.Response('not found', 404);
    }));
    await client.login('member', 'password');
    await tester.pumpWidget(MaterialApp(home: RenewalPage(apiClient: client)));
    await tester.pumpAndSettle();
    expect(find.text('300.000 VND'), findsOneWidget);
    expect(find.text('Xác nhận gia hạn'), findsOneWidget);
    await tester.tap(find.text('Xác nhận gia hạn'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.text('Gia hạn đang chờ thanh toán'), findsOneWidget);
    expect(find.text('Mã QR giao dịch'), findsOneWidget);
  });
}
