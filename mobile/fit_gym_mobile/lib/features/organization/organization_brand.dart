class OrganizationBrand {
  const OrganizationBrand({required this.displayName, required this.hasLogo});

  static const fallback = OrganizationBrand(
    displayName: 'Fit_Gym',
    hasLogo: false,
  );

  final String displayName;
  final bool hasLogo;

  factory OrganizationBrand.fromJson(Map<String, dynamic> json) {
    if (json['isConfigured'] != true) return fallback;
    final name = (json['displayName'] as String?)?.trim();
    return OrganizationBrand(
      displayName: name == null || name.isEmpty ? 'Fit_Gym' : name,
      hasLogo: json['hasLogo'] == true,
    );
  }
}
