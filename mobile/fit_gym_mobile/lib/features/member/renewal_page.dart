import 'dart:math';
import 'dart:typed_data';

import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import 'renewal_models.dart';

class RenewalPage extends StatefulWidget {
  const RenewalPage({super.key, required this.apiClient});

  final ApiClient apiClient;

  @override
  State<RenewalPage> createState() => _RenewalPageState();
}

class _RenewalPageState extends State<RenewalPage> {
  List<MemberPackage> _packages = const [];
  MemberPackage? _selected;
  RenewalQuote? _quote;
  RenewalResult? _result;
  String? _requestKey;
  String? _error;
  bool _loading = true;
  bool _submitting = false;
  int? _sourceRegistrationId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final registrations = await widget.apiClient.loadSubscriptions();
      final renewable = registrations.where((item) => item.canRenew).toList()
        ..sort((a, b) => b.endDate.compareTo(a.endDate));
      if (renewable.isEmpty) {
        throw const ApiException('Không tìm thấy gói tập đủ điều kiện gia hạn.');
      }
      final packages = await widget.apiClient.loadPackages();
      if (!mounted) return;
      setState(() {
        _packages = packages;
        _sourceRegistrationId = renewable.first.registrationId;
        _loading = false;
      });
      if (packages.isNotEmpty) await _selectPackage(packages.first);
    } on ApiException catch (error) {
      if (mounted) setState(() { _error = error.message; _loading = false; });
    } catch (_) {
      if (mounted) setState(() { _error = 'Không thể tải thông tin gia hạn.'; _loading = false; });
    }
  }

  Future<void> _selectPackage(MemberPackage package) async {
    setState(() {
      _selected = package;
      _quote = null;
      _requestKey = null;
      _error = null;
    });
    try {
      final quote = await widget.apiClient.quoteRenewal(_sourceRegistrationId!, package.packageId);
      if (mounted && _selected?.packageId == package.packageId) {
        setState(() => _quote = quote);
      }
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } catch (_) {
      if (mounted) setState(() => _error = 'Không thể tính thời hạn gia hạn.');
    }
  }

  Future<void> _submit() async {
    final package = _selected;
    final quote = _quote;
    if (package == null || quote == null || _submitting) return;
    _requestKey ??= List<int>.generate(16, (_) => Random.secure().nextInt(256))
        .map((byte) => byte.toRadixString(16).padLeft(2, '0')).join();
    setState(() { _submitting = true; _error = null; });
    try {
      final result = await widget.apiClient.createRenewal(
        _sourceRegistrationId!, package.packageId, quote, _requestKey!);
      if (mounted) setState(() => _result = result);
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } catch (_) {
      if (mounted) setState(() => _error = 'Không thể gửi yêu cầu gia hạn.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final result = _result;
    return Scaffold(
      appBar: AppBar(title: const Text('Gia hạn gói tập')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(20),
              children: [
                if (result != null) ...[
                  const Text('Gia hạn đang chờ thanh toán',
                      style: TextStyle(fontSize: 20, fontWeight: FontWeight.w600)),
                  if (_quote != null) ...[
                    const SizedBox(height: 12),
                    _detail('Tổng tiền', _money(_quote!.totalAmount)),
                  ],
                  const SizedBox(height: 16),
                  PaymentQrCard(apiClient: widget.apiClient, paymentId: result.paymentId,
                    requestCode: result.paymentRequestCode),
                  const SizedBox(height: 16),
                  FilledButton(onPressed: () => Navigator.pop(context), child: const Text('Hoàn tất')),
                ] else ...[
                  const Text('Chọn gói tập', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w600)),
                  const SizedBox(height: 16),
                  if (_packages.isEmpty) const Text('Chưa có gói tập đang cung cấp.'),
                  if (_packages.isNotEmpty)
                    DropdownButtonFormField<int>(
                      initialValue: _selected?.packageId,
                      decoration: const InputDecoration(labelText: 'Gói tập'),
                      items: _packages.map((item) => DropdownMenuItem(
                        value: item.packageId, child: Text(item.name))).toList(),
                      onChanged: (id) {
                        final package = _packages.where((item) => item.packageId == id).first;
                        _selectPackage(package);
                      },
                    ),
                  const SizedBox(height: 16),
                  if (_quote != null) Card(child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      _detail('Bắt đầu', _date(_quote!.startDate)),
                      _detail('Kết thúc', _date(_quote!.endDate)),
                      const Divider(height: 28),
                      _detail('Tổng tiền', _money(_quote!.totalAmount)),
                    ]),
                  )),
                  if (_selected != null && _quote == null && _error == null)
                    const Center(child: Padding(padding: EdgeInsets.all(20), child: CircularProgressIndicator())),
                  if (_error != null) Padding(padding: const EdgeInsets.symmetric(vertical: 16),
                    child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
                  const SizedBox(height: 16),
                  FilledButton(onPressed: _quote == null || _submitting ? null : _submit,
                    child: Text(_submitting ? 'Đang gửi…' : 'Xác nhận gia hạn')),
                ],
              ],
            ),
    );
  }
}

class PaymentQrCard extends StatelessWidget {
  const PaymentQrCard({super.key, required this.apiClient, required this.paymentId,
    required this.requestCode});

  final ApiClient apiClient;
  final int paymentId;
  final String requestCode;

  @override
  Widget build(BuildContext context) {
    return Card(child: Padding(
      padding: const EdgeInsets.all(20),
      child: Column(children: [
        const Text('Mã QR giao dịch', style: TextStyle(fontSize: 17, fontWeight: FontWeight.w600)),
        const SizedBox(height: 16),
        FutureBuilder<List<int>>(
          future: apiClient.loadPaymentQr(paymentId),
          builder: (context, snapshot) {
            if (snapshot.hasError) return const Text('Không thể tải mã QR. Vui lòng tải lại trang.');
            if (!snapshot.hasData) return const SizedBox(height: 180, child: Center(child: CircularProgressIndicator()));
            return Image.memory(Uint8List.fromList(snapshot.data!), width: 180, height: 180);
          },
        ),
        const SizedBox(height: 12),
        Text('Mã yêu cầu: $requestCode', style: const TextStyle(color: AppTheme.muted)),
      ]),
    ));
  }
}

Widget _detail(String label, String value) => Padding(
  padding: const EdgeInsets.symmetric(vertical: 5),
  child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
    Text(label, style: const TextStyle(color: AppTheme.muted)),
    Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
  ]),
);

String _date(DateTime value) => '${value.day.toString().padLeft(2, '0')}/${value.month.toString().padLeft(2, '0')}/${value.year}';

String _money(double value) {
  final digits = value.toStringAsFixed(0);
  return '${digits.replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => '.')} VND';
}
