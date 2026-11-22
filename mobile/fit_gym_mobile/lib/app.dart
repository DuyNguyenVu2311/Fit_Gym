import 'package:flutter/material.dart';

import 'core/network/api_client.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/login_page.dart';
import 'features/home/member_home_page.dart';
import 'features/organization/organization_brand.dart';

class FitGymApp extends StatefulWidget {
  const FitGymApp({super.key, this.apiClient});

  final ApiClient? apiClient;

  @override
  State<FitGymApp> createState() => _FitGymAppState();
}

class _FitGymAppState extends State<FitGymApp> {
  late final ApiClient _apiClient = widget.apiClient ?? ApiClient();
  bool _authenticated = false;
  OrganizationBrand _brand = OrganizationBrand.fallback;

  @override
  void initState() {
    super.initState();
    _loadBrand();
  }

  Future<void> _loadBrand() async {
    try {
      final brand = await _apiClient.loadBrand();
      if (mounted) setState(() => _brand = brand);
    } catch (_) {
      // Branding is optional; a temporary outage must not block sign-in.
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: _brand.displayName,
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      home: _authenticated
          ? MemberHomePage(
              organizationName: _brand.displayName,
              apiClient: _apiClient,
              onLogout: () {
                _apiClient.logout();
                setState(() => _authenticated = false);
              },
            )
          : LoginPage(
              organizationName: _brand.displayName,
              logoUrl: _brand.hasLogo
                  ? '${ApiClient.baseUrl}/api/organization/logo'
                  : null,
              apiClient: _apiClient,
              onLoggedIn: () => setState(() => _authenticated = true),
            ),
    );
  }
}
