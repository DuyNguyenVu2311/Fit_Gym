import 'renewal_models.dart';

class MemberProfile {
  const MemberProfile({
    required this.fullName,
    required this.phoneNumber,
    required this.joinedDate,
    required this.status,
    required this.hasActiveBiometricConsent,
    required this.hasActiveFaceData,
  });

  factory MemberProfile.fromJson(Map<String, dynamic> json) => MemberProfile(
    fullName: json['fullName'] as String,
    phoneNumber: json['phoneNumber'] as String,
    joinedDate: DateTime.parse(json['joinedDate'] as String),
    status: json['status'] as String,
    hasActiveBiometricConsent: json['hasActiveBiometricConsent'] as bool,
    hasActiveFaceData: json['hasActiveFaceData'] as bool,
  );

  final String fullName;
  final String phoneNumber;
  final DateTime joinedDate;
  final String status;
  final bool hasActiveBiometricConsent;
  final bool hasActiveFaceData;
}

class CurrentSubscription {
  const CurrentSubscription({
    required this.registrationId,
    required this.packageName,
    required this.startDate,
    required this.endDate,
    required this.totalAmount,
    required this.registrationStatus,
    this.paymentMethod,
    this.paymentStatus,
    this.paymentRequestCode,
  });

  factory CurrentSubscription.fromJson(Map<String, dynamic> json) =>
      CurrentSubscription(
        registrationId: json['registrationId'] as int,
        packageName: json['packageName'] as String,
        startDate: DateTime.parse(json['startDate'] as String),
        endDate: DateTime.parse(json['endDate'] as String),
        totalAmount: (json['totalAmount'] as num).toDouble(),
        registrationStatus: json['registrationStatus'] as String,
        paymentMethod: json['paymentMethod'] as String?,
        paymentStatus: json['paymentStatus'] as String?,
        paymentRequestCode: json['paymentRequestCode'] as String?,
      );

  final int registrationId;
  final String packageName;
  final DateTime startDate;
  final DateTime endDate;
  final double totalAmount;
  final String registrationStatus;
  final String? paymentMethod;
  final String? paymentStatus;
  final String? paymentRequestCode;
}

class MemberPayment {
  const MemberPayment({
    required this.paymentId,
    required this.registrationId,
    required this.amount,
    required this.paymentDate,
    required this.method,
    required this.status,
    required this.requestCode,
  });
  factory MemberPayment.fromJson(Map<String, dynamic> json) => MemberPayment(
    paymentId: json['paymentId'] as int,
    registrationId: json['registrationId'] as int,
    amount: (json['amount'] as num).toDouble(),
    paymentDate: DateTime.parse(json['paymentDate'] as String).toLocal(),
    method: json['method'] as String,
    status: json['status'] as String,
    requestCode: json['requestCode'] as String,
  );
  final int paymentId;
  final int registrationId;
  final double amount;
  final DateTime paymentDate;
  final String method;
  final String status;
  final String requestCode;
}

class MemberCheckIn {
  const MemberCheckIn({
    required this.checkedInAt,
    required this.method,
    required this.status,
  });
  factory MemberCheckIn.fromJson(Map<String, dynamic> json) => MemberCheckIn(
    checkedInAt: DateTime.parse(json['checkedInAt'] as String),
    method: json['method'] as String,
    status: json['status'] as String,
  );
  final DateTime checkedInAt;
  final String method;
  final String status;
}

class MemberDashboardData {
  const MemberDashboardData({
    required this.profile,
    required this.subscription,
    required this.subscriptions,
    required this.payments,
    required this.checkIns,
  });
  final MemberProfile profile;
  final CurrentSubscription? subscription;
  final List<MemberRegistration> subscriptions;
  final List<MemberPayment> payments;
  final List<MemberCheckIn> checkIns;

  bool get canRenew => subscriptions.any((item) => item.canRenew) &&
      !subscriptions.any((item) => item.paymentStatus == 'ChoThanhToan');
}
