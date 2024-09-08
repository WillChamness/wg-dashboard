import os
import random
import unittest
import requests
import dotenv
import jwt
import urllib3

class TestApiAuth(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        dotenv.load_dotenv()
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        cls.url = os.getenv("API_URL") + "/api/auth"
        cls.claims_names = (
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        )
        cls.session = requests.Session()
        cls.session.verify = False


    # in case you want to rerun the test without reloading the project
    def rand_username(self): 
        return "myuser" + str(random.randint(0, 10**9))


    def test_signup_and_login(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)
        
        response = self.session.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode()
        self.assertIsNotNone(encoded_jwt)


    def test_signup_with_name(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword",
            "name": "My User"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_cannot_add_same_username(self):
        credentials = {
            "username": self.rand_username(),
            "password": "password"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        credentials["password"] = "this is another user with the same username"
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)


    def test_change_password(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        response = self.session.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
        self.assertIsInstance(decoded_jwt, dict)

        userid = -1
        for claim in self.claims_names:
            self.assertIsInstance(decoded_jwt[claim], str)
            if "sid" in claim:
                userid = int(decoded_jwt[claim]) # check to make sure that the result is an int

        # change password
        response = self.session.patch(self.url + "/passwd/" + str(userid), json={
                "id": userid,
                "password": "mynewpassword"
            },
            headers={"Authorization": "Bearer " + encoded_jwt}
        )
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        # make sure new password works
        self.session.post(self.url + "/login", json={
            "username": credentials["username"],
            "password": "mynewpassword"
         })
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        # make sure old password doesn't work
        response = self.session.post(self.url + "/login", json=credentials)
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)



    def test_unauthorized_password_change(self):
        credentials1 = {
            "username": self.rand_username(),
            "password": "mypassword"
        }
        credentials2 = {
            "username": self.rand_username(),
            "password": "mypassword"
        }

        self.session.post(self.url + "/signup", json=credentials1)
        response = self.session.post(self.url + "/login", json=credentials1)
        encoded_jwt1 = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt1, algorithms=["HS256"], options={"verify_signature": False})
        self.assertIsInstance(decoded_jwt, dict)
        user1id = int(decoded_jwt[self.claims_names[0]])
        
        self.session.post(self.url + "/signup", json=credentials2)
        response = self.session.post(self.url + "/login", json=credentials2)
        encoded_jwt2 = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt1, algorithms=["HS256"], options={"verify_signature": False})
        self.assertIsInstance(decoded_jwt, dict)
        user2id = int(decoded_jwt[self.claims_names[0]])

        # unauthorized password change
        response = self.session.patch(self.url + "/passwd/" + str(user1id),
            headers={"Authorization": "Bearer " + encoded_jwt2},
            json={"id": user1id, "password": "mynewpassword"}
        )
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)

        # make sure user can still login
        response = self.session.post(self.url + "/login", json=credentials1)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_admin_change_password(self):
        admin_credentials = {"username": "admin", "password": "admin"}
        credentials = {"username": self.rand_username(), "password": "mypassword"}

        self.session.post(self.url + "/signup", json=credentials)
        response = self.session.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
        userid = int(decoded_jwt[self.claims_names[0]])

        response = self.session.post(self.url + "/login", json=admin_credentials)
        admin_jwt = response.content.decode().replace("\"", "")

        # change password
        response = self.session.patch(self.url + "/passwd/" + str(userid),
            headers={"Authorization": "Bearer " + admin_jwt},
            json={"id": userid, "password": "mynewpassword"}
        )
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        # make sure new password works
        response = self.session.post(self.url + "/login", 
            json={"username": credentials["username"], "password": "mynewpassword"}
        )
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        # make sure old password doesn't work
        response = self.session.post(self.url + "/login", json=credentials)
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)



if __name__ == '__main__':
    unittest.main()
