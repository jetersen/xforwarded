#!/usr/bin/env python3
"""Black-box regression checks shared by managed, NativeAOT, and container runs."""
import json
import sys
import time
import unittest
from urllib.request import Request, urlopen

BASE = sys.argv.pop(1).rstrip('/') if len(sys.argv) > 1 else 'http://127.0.0.1:8080'


def get(path, headers=None):
    with urlopen(Request(BASE + path, headers=headers or {}), timeout=5) as response:
        return response.status, response.read().decode()


class DiagnosticsTests(unittest.TestCase):
    def diagnostic(self, path, headers=None):
        status, body = get(path, headers)
        self.assertEqual(status, 200)
        result = json.loads(body)
        self.assertEqual(set(result), {'remotePort', 'remoteIp', 'host', 'scheme', 'headers'})
        result['headers'] = {k.lower(): v for k, v in result['headers'].items()}
        return result

    def test_health(self):
        for path in ['/readiness', '/liveness']:
            self.assertEqual(get(path), (200, 'OK'))

    def test_route_specific_forwarding(self):
        headers = {
            'Host': 'origin.example:8080',
            'X-Forwarded-For': '198.51.100.7:4000, 203.0.113.8:5000',
            'X-Forwarded-Host': 'viewer.example',
            'X-Forwarded-Proto': 'https',
            'X-Original-Forwarded-For': '192.0.2.1:6000',
            'X-Forwarded-Port': '7000',
        }
        for path, forwarded, port in [('/', False, None), ('/forwardedHeaders', True, 5000),
                                     ('/forwardedPort', False, 6000),
                                     ('/forwardedHeadersAndPort', True, 6000)]:
            with self.subTest(path=path):
                data = self.diagnostic(path, headers)
                self.assertEqual(data['host'], 'viewer.example' if forwarded else 'origin.example')
                self.assertEqual(data['scheme'], 'https' if forwarded else 'http')
                if forwarded:
                    self.assertEqual(data['remoteIp'], '203.0.113.8')
                    self.assertEqual(data['headers']['x-forwarded-for'], ['198.51.100.7:4000'])
                    self.assertIn('x-original-for', data['headers'])
                else:
                    self.assertNotEqual(data['remoteIp'], '203.0.113.8')
                if port is not None:
                    self.assertEqual(data['remotePort'], port)
                else:
                    self.assertGreater(data['remotePort'], 0)
                    self.assertEqual(data['headers']['x-forwarded-port'], ['7000'])

    def test_port_fallback_and_invalid_input(self):
        for path in ['/forwardedPort', '/forwardedHeadersAndPort']:
            self.assertEqual(self.diagnostic(path, {'X-Forwarded-Port': '7000'})['remotePort'], 7000)
            data = self.diagnostic(path, {'X-Original-Forwarded-For': 'invalid', 'X-Forwarded-Port': 'invalid'})
            self.assertGreater(data['remotePort'], 0)

    def test_ipv6_forwarded_address(self):
        data = self.diagnostic('/forwardedHeaders', {'X-Forwarded-For': '[2001:db8::1]:4321'})
        self.assertEqual(data['remoteIp'], '2001:db8::1')
        self.assertEqual(data['remotePort'], 4321)

    def test_header_redaction(self):
        sensitive = ['aUtHoRiZaTiOn', 'Proxy-Authorization', 'Cookie', 'Set-Cookie',
                     'X-Api-Key', 'Api-Key', 'X-Auth-Token', 'X-Forwarded-Access-Token',
                     'X-Amzn-Oidc-Accesstoken', 'X-Amzn-Oidc-Data', 'X-Custom-Secret']
        for path in ['/', '/forwardedHeaders', '/forwardedPort', '/forwardedHeadersAndPort']:
            headers = dict.fromkeys(sensitive, 'test-secret')
            headers['X-Diagnostic-Test'] = 'visible'
            data = self.diagnostic(path, headers)
            for name in sensitive:
                self.assertEqual(data['headers'][name.lower()], ['[REDACTED]'])
            self.assertEqual(data['headers']['x-diagnostic-test'], ['visible'])
            self.assertNotIn('test-secret', json.dumps(data))


if __name__ == '__main__':
    for attempt in range(60):
        try:
            get('/readiness')
            break
        except OSError:
            time.sleep(0.5)
    unittest.main()
