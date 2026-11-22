import 'package:fit_gym_mobile/core/network/api_client.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test('loads only public branding from this installation API', () async {
    final client = ApiClient(
      client: MockClient((request) async {
        expect(request.url.path, '/api/organization');
        expect(request.headers.containsKey('Authorization'), false);
        return http.Response(
          '{"isConfigured":true,"displayName":"Gym của đơn vị A","siteName":"Cơ sở đầu tiên","hasLogo":true}',
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      }),
    );
    final brand = await client.loadBrand();
    expect(brand.displayName, 'Gym của đơn vị A');
    expect(brand.hasLogo, true);
  });
}
