import os
import unittest
import requests
import dotenv

class TestApiConnection(unittest.TestCase):
    def setUp(self):
        dotenv.load_dotenv()
        self.url = os.getenv("API_URL")
        

    def test_can_get_response(self):
        response = requests.get(self.url + "/api")
        self.assertTrue(100 <= response.status_code and response.status_code <= 599)


    def test_development_admin_profile(self):
        response = requests.post(self.url + "/api/auth/login", json={
            "username": "admin",
            "password": "admin",
        })
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


if __name__ == '__main__':
    unittest.main()
