import 'package:fit_gym_mobile/app.dart';
import 'package:fit_gym_mobile/core/network/api_client.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  testWidgets('branding outage keeps the member login usable', (tester) async {
    await tester.pumpWidget(
      FitGymApp(
        apiClient: ApiClient(
          client: MockClient(
            (_) async => throw http.ClientException('offline'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Fit_Gym'), findsOneWidget);
    expect(find.text('Đăng nhập'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('shows the member login screen', (tester) async {
    await tester.pumpWidget(
      FitGymApp(
        apiClient: ApiClient(
          client: MockClient(
            (_) async => http.Response('{"isConfigured":false}', 200),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Fit_Gym'), findsOneWidget);
    expect(find.text('Đăng nhập tài khoản hội viên'), findsOneWidget);
    expect(find.text('Tên đăng nhập hoặc email'), findsOneWidget);
    expect(find.text('Mật khẩu'), findsOneWidget);
    expect(find.text('Đăng nhập'), findsOneWidget);
  });

  testWidgets('shows the configured customer name without a tenant selector', (
    tester,
  ) async {
    await tester.pumpWidget(
      FitGymApp(
        apiClient: ApiClient(
          client: MockClient(
            (_) async => http.Response(
              '{"isConfigured":true,"displayName":"Gym A","hasLogo":false}',
              200,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Gym A'), findsOneWidget);
    expect(find.text('Đăng nhập'), findsOneWidget);
  });
}
