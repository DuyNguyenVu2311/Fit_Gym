class MemberRegistration {
  const MemberRegistration({
    required this.registrationId,
    required this.packageName,
    required this.startDate,
    required this.endDate,
    required this.registrationStatus,
    required this.paymentStatus,
    required this.validity,
  });

  factory MemberRegistration.fromJson(Map<String, dynamic> json) => MemberRegistration(
        registrationId: json['registrationId'] as int,
        packageName: json['packageName'] as String,
        startDate: DateTime.parse(json['startDate'] as String),
        endDate: DateTime.parse(json['endDate'] as String),
        registrationStatus: json['registrationStatus'] as String,
        paymentStatus: json['paymentStatus'] as String?,
        validity: json['validity'] as String,
      );

  final int registrationId;
  final String packageName;
  final DateTime startDate;
  final DateTime endDate;
  final String registrationStatus;
  final String? paymentStatus;
  final String validity;

  bool get isUsable => validity == 'Usable';
  bool get canRenew => registrationStatus == 'HieuLuc' && paymentStatus == 'ThanhCong';
}

class MemberPackage {
  const MemberPackage({required this.packageId, required this.name,
    required this.durationMonths, required this.price});

  factory MemberPackage.fromJson(Map<String, dynamic> json) => MemberPackage(
        packageId: json['packageId'] as int,
        name: json['name'] as String,
        durationMonths: json['durationMonths'] as int,
        price: (json['price'] as num).toDouble(),
      );

  final int packageId;
  final String name;
  final int durationMonths;
  final double price;
}

class RenewalQuote {
  const RenewalQuote({required this.startDate, required this.endDate,
    required this.totalAmount});

  factory RenewalQuote.fromJson(Map<String, dynamic> json) => RenewalQuote(
        startDate: DateTime.parse(json['startDate'] as String),
        endDate: DateTime.parse(json['endDate'] as String),
        totalAmount: (json['totalAmount'] as num).toDouble(),
      );

  final DateTime startDate;
  final DateTime endDate;
  final double totalAmount;
}

class RenewalResult {
  const RenewalResult({required this.registrationId, required this.paymentId,
    required this.paymentRequestCode});

  factory RenewalResult.fromJson(Map<String, dynamic> json) => RenewalResult(
        registrationId: json['registrationId'] as int,
        paymentId: json['paymentId'] as int,
        paymentRequestCode: json['paymentRequestCode'] as String,
      );

  final int registrationId;
  final int paymentId;
  final String paymentRequestCode;
}
