import 'package:flutter/material.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../member/member_models.dart';
import '../member/renewal_page.dart';

class MemberHomePage extends StatefulWidget {
  const MemberHomePage({
    super.key,
    required this.apiClient,
    required this.onLogout,
    this.organizationName = 'Fit_Gym',
  });

  final ApiClient apiClient;
  final VoidCallback onLogout;
  final String organizationName;

  @override
  State<MemberHomePage> createState() => _MemberHomePageState();
}

class _MemberHomePageState extends State<MemberHomePage> {
  MemberDashboardData? _data;
  String? _error;
  bool _loading = true;
  int _tabIndex = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final data = await widget.apiClient.loadDashboard();
      if (mounted) setState(() => _data = data);
    } on ApiException catch (exception) {
      if (mounted) setState(() => _error = exception.message);
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Không thể tải dữ liệu hội viên.');
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          widget.organizationName,
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        actions: [
          IconButton(
            onPressed: _load,
            tooltip: 'Làm mới',
            icon: const Icon(Icons.refresh),
          ),
          IconButton(
            onPressed: widget.onLogout,
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: _buildBody(),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tabIndex,
        onDestinationSelected: (index) => setState(() => _tabIndex = index),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.home_outlined),
            label: 'Tổng quan',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            label: 'Thanh toán',
          ),
          NavigationDestination(
            icon: Icon(Icons.login_outlined),
            label: 'Lượt vào',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            label: 'Cá nhân',
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.cloud_off_outlined, size: 52),
              const SizedBox(height: 12),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              FilledButton.tonal(
                onPressed: _load,
                child: const Text('Thử lại'),
              ),
            ],
          ),
        ),
      );
    }
    final data = _data!;
    return RefreshIndicator(
      onRefresh: _load,
      child: IndexedStack(
        index: _tabIndex,
        children: [
          _OverviewTab(data: data, onRenew: _openRenewal),
          _PaymentsTab(payments: data.payments, apiClient: widget.apiClient),
          _CheckInsTab(checkIns: data.checkIns),
          _ProfileTab(profile: data.profile),
        ],
      ),
    );
  }

  Future<void> _openRenewal() async {
    await Navigator.of(context).push(MaterialPageRoute<void>(
      builder: (_) => RenewalPage(apiClient: widget.apiClient),
    ));
    if (mounted) await _load();
  }
}

class _OverviewTab extends StatelessWidget {
  const _OverviewTab({required this.data, required this.onRenew});
  final MemberDashboardData data;
  final VoidCallback onRenew;

  @override
  Widget build(BuildContext context) {
    final subscription = data.subscription;
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(
          data.profile.fullName,
          style: Theme.of(context).textTheme.headlineSmall
              ?.copyWith(fontWeight: FontWeight.w500),
        ),
        const SizedBox(height: 16),
        Card(
          color: subscription == null ? Colors.white : AppTheme.rail,
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: subscription == null
                ? const _EmptyState(
                    icon: Icons.card_membership_outlined,
                    message: 'Chưa có gói tập hiện hành.',
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Gói tập hiện tại',
                        style: TextStyle(color: Color(0xFFC6CDC4)),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        subscription.packageName,
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(
                              color: Colors.white,
                              fontWeight: FontWeight.w500,
                            ),
                      ),
                      const SizedBox(height: 24),
                      _InfoRow(
                        label: 'Bắt đầu',
                        value: _date(subscription.startDate),
                        onDark: true,
                      ),
                      _InfoRow(
                        label: 'Kết thúc',
                        value: _date(subscription.endDate),
                        onDark: true,
                      ),
                      _InfoRow(
                        label: 'Trạng thái',
                        value: subscription.registrationStatus,
                        onDark: true,
                      ),
                      if (subscription.paymentStatus != null)
                        _InfoRow(
                          label: 'Thanh toán',
                          value: subscription.paymentStatus!,
                          onDark: true,
                        ),
                    ],
                  ),
          ),
        ),
        const SizedBox(height: 16),
        if (data.canRenew)
          FilledButton(onPressed: onRenew, child: const Text('Gia hạn gói tập')),
      ],
    );
  }
}

class _PaymentsTab extends StatelessWidget {
  const _PaymentsTab({required this.payments, required this.apiClient});
  final List<MemberPayment> payments;
  final ApiClient apiClient;

  @override
  Widget build(BuildContext context) {
    if (payments.isEmpty) {
      return ListView(
        children: [
          SizedBox(height: 160),
          _EmptyState(
            icon: Icons.receipt_long,
            message: 'Chưa có lịch sử thanh toán.',
          ),
        ],
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: payments.length,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (context, index) {
        final payment = payments[index];
        return Card(
          child: ListTile(
            leading: const Icon(Icons.payments_outlined, color: AppTheme.muted),
            title: Text(
              _money(payment.amount),
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
            subtitle: Text('${_date(payment.paymentDate)} · ${payment.method == 'TienMat' ? 'Tiền mặt' : payment.method}'),
            trailing: Text(payment.status == 'ThanhCong' ? 'Đã thanh toán' :
              payment.status == 'ChoThanhToan' ? 'Chờ xác nhận' : 'Thất bại'),
            onTap: payment.status == 'ChoThanhToan' && payment.method == 'QR'
                ? () => showModalBottomSheet<void>(context: context, isScrollControlled: true,
                    builder: (_) => SafeArea(child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: PaymentQrCard(apiClient: apiClient, paymentId: payment.paymentId,
                        requestCode: payment.requestCode),
                    )))
                : null,
          ),
        );
      },
    );
  }
}

class _CheckInsTab extends StatelessWidget {
  const _CheckInsTab({required this.checkIns});
  final List<MemberCheckIn> checkIns;

  @override
  Widget build(BuildContext context) {
    if (checkIns.isEmpty) {
      return ListView(
        children: [
          SizedBox(height: 160),
          _EmptyState(
            icon: Icons.login,
            message: 'Chưa có lượt vào trong 30 ngày.',
          ),
        ],
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: checkIns.length,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (context, index) {
        final checkIn = checkIns[index];
        return Card(
          child: ListTile(
            leading: const Icon(Icons.login_outlined, color: AppTheme.muted),
            title: Text(
              _dateTime(checkIn.checkedInAt),
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
            subtitle: Text(checkIn.method == 'KhuonMat' ? 'Khuôn mặt' : 'Nhân viên hỗ trợ'),
          ),
        );
      },
    );
  }
}

class _ProfileTab extends StatelessWidget {
  const _ProfileTab({required this.profile});
  final MemberProfile profile;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              children: [
                const CircleAvatar(
                  radius: 34,
                  child: Icon(Icons.person, size: 36),
                ),
                const SizedBox(height: 12),
                Text(
                  profile.fullName,
                  style: Theme.of(context).textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w500),
                ),
                const SizedBox(height: 18),
                _InfoRow(label: 'Số điện thoại', value: profile.phoneNumber),
                _InfoRow(
                  label: 'Ngày tham gia',
                  value: _date(profile.joinedDate),
                ),
                _InfoRow(label: 'Trạng thái', value: profile.status),
                _InfoRow(
                  label: 'Đồng ý sinh trắc học',
                  value: profile.hasActiveBiometricConsent
                      ? 'Còn hiệu lực'
                      : 'Chưa có',
                ),
                _InfoRow(
                  label: 'Dữ liệu khuôn mặt',
                  value: profile.hasActiveFaceData
                      ? 'Đang hoạt động'
                      : 'Chưa có',
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.label,
    required this.value,
    this.onDark = false,
  });
  final String label;
  final String value;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                color: onDark ? const Color(0xFFC6CDC4) : AppTheme.muted,
              ),
            ),
          ),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: TextStyle(
                color: onDark ? Colors.white : AppTheme.ink,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.icon, required this.message});
  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 40, color: AppTheme.muted),
            const SizedBox(height: 10),
            Text(message, textAlign: TextAlign.center),
          ],
        ),
      ),
    );
  }
}

String _twoDigits(int value) => value.toString().padLeft(2, '0');
String _date(DateTime value) =>
    '${_twoDigits(value.day)}/${_twoDigits(value.month)}/${value.year}';
String _dateTime(DateTime value) =>
    '${_date(value)} · ${_twoDigits(value.hour)}:${_twoDigits(value.minute)}';
String _money(double value) {
  final parts = value.toStringAsFixed(2).split('.');
  final digits = parts[0];
  final buffer = StringBuffer();
  for (var index = 0; index < digits.length; index++) {
    if (index > 0 && (digits.length - index) % 3 == 0) buffer.write('.');
    buffer.write(digits[index]);
  }
  final fraction = parts[1].replaceFirst(RegExp(r'0+$'), '');
  return '${buffer.toString()}${fraction.isEmpty ? '' : ',$fraction'} VND';
}
