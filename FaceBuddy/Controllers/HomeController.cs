using FacebookLoginASPnetWebForms.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace FaceBuddy.Controllers
{
    public class HomeController : Controller
    {
        public string appID = "";
        public string secretID = "";
        public ActionResult Index()
        {
            string url = "https://www.facebook.com/v2.4/dialog/oauth/?client_id="
                + "581851792018041"
                + "&redirect_uri=http://" + "localhost:7767"
                + "/Home/AppLogin&response_type=code&state=1&scope=public_profile,user_posts,user_friends,user_tagged_places,publish_actions";
            return Redirect(url);
        }

        public ActionResult AppLogin(string code)
        {
            Uri targetUri = new Uri("https://graph.facebook.com/oauth/access_token?client_id=" + "581851792018041" + "&client_secret=" + "99dc72851691d7386c59414bac32b8fe" + "&redirect_uri=http://" + Request.ServerVariables["SERVER_NAME"] + ":" + Request.ServerVariables["SERVER_PORT"] + "/Home/AppLogin&code=" + code);
            HttpWebRequest at = (HttpWebRequest)HttpWebRequest.Create(targetUri);

            System.IO.StreamReader str = new System.IO.StreamReader(at.GetResponse().GetResponseStream());
            string token = str.ReadToEnd().ToString().Replace("access_token=", "");

            // Split the access token and expiration from the single string
            string[] combined = token.Split('&');
            string accessToken = combined[0];

            // Exchange the code for an extended access token
            Uri eatTargetUri = new Uri("https://graph.facebook.com/oauth/access_token?grant_type=fb_exchange_token&client_id=" + "581851792018041&client_secret=" + "99dc72851691d7386c59414bac32b8fe&fb_exchange_token=" + accessToken);
            HttpWebRequest eat = (HttpWebRequest)HttpWebRequest.Create(eatTargetUri);

            StreamReader eatStr = new StreamReader(eat.GetResponse().GetResponseStream());
            string eatToken = eatStr.ReadToEnd().ToString().Replace("access_token=", "");


            // Split the access token and expiration from the single string
            string[] eatWords = eatToken.Split('&');
            string extendedAccessToken = eatWords[0];

            // Request the Facebook user information
            Uri targetUserUri = new Uri("https://graph.facebook.com/me?fields=first_name,last_name,gender,locale,link&access_token=" + accessToken);
            HttpWebRequest user = (HttpWebRequest)HttpWebRequest.Create(targetUserUri);

            // Read the returned JSON object response
            StreamReader userInfo = new StreamReader(user.GetResponse().GetResponseStream());
            string jsonResponse = string.Empty;
            jsonResponse = userInfo.ReadToEnd();

            // Deserialize and convert the JSON object to the Facebook.User object type
            JavaScriptSerializer sr = new JavaScriptSerializer();
            string jsondata = jsonResponse;
            facebook.User converted = sr.Deserialize<facebook.User>(jsondata);
            converted.accessToken = eatToken;

            // Write the user data to a List
            List<facebook.User> currentUser = new List<facebook.User>();
            currentUser.Add(converted);
            Session["facebooktoken"] = eatToken;
            Session["id"] = currentUser[0].id;
            //test(eatToken, currentUser[0].id);
            return View("Index");
        }

        [HttpPost]
        public string Speak(string param)
        {
            return readPost(Session["facebooktoken"].ToString(), Session["id"].ToString());
            //return "Hello, my name is Halloween";
        }

        [HttpPost]
        public string PostStatus(string status)
        {
            string[] list = status.Split(' ');
            string st = string.Empty;
            for (int i = 1; i < list.Length; i++)
                st += list[i] + " ";
            Facebook.FacebookClient fbClient = new Facebook.FacebookClient(Session["facebooktoken"].ToString());
            fbClient.Post("/me/feed", new { message = st });
            return st;
        }

        #region Private Functions

        private void getDetails(UserPosts userPost, string accessToken, string id)
        {
            Uri commentUri = new Uri("https://graph.facebook.com/" + userPost.id + "?fields=from&access_token=" + accessToken);
            HttpWebRequest commentRequest = (HttpWebRequest)HttpWebRequest.Create(commentUri);
            StreamReader commentRes = new StreamReader(commentRequest.GetResponse().GetResponseStream());
            JavaScriptSerializer sr = new JavaScriptSerializer();
            string commentJson = commentRes.ReadToEnd();
            JObject jobject = JObject.Parse(commentJson);
            NodeDestination from = sr.Deserialize<NodeDestination>(jobject["from"].ToString());
            userPost.from = from;
        }
        static string GetDescription(string url)
        {
            string result = "";
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["maxCandidates"] = "1";
            var uri = "https://api.projectoxford.ai/vision/v1.0/describe";
            HttpWebRequest msRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            msRequest.Headers.Add("Ocp-Apim-Subscription-Key", "67e9c741ef29482b9809bfd94eb17e81");
            msRequest.ContentType = "application/json";
            msRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes("{\"url\":\"" + url + "\"}");
            msRequest.ContentLength = bytes.Length;
            Stream os = msRequest.GetRequestStream();
            os.Write(bytes, 0, bytes.Length);
            os.Close();
            WebResponse re = msRequest.GetResponse();
            StreamReader reader = new StreamReader(re.GetResponseStream());
            string r = reader.ReadToEnd();
            JObject jobject = JObject.Parse(r);
            result = jobject["description"]["captions"][0]["text"].ToString();
            return result;
        }
        private void getAttachment(UserPosts userPost, string accessToken)
        {
            Uri commentUri = new Uri("https://graph.facebook.com/" + userPost.id + "/attachments?access_token=" + accessToken);
            HttpWebRequest commentRequest = (HttpWebRequest)HttpWebRequest.Create(commentUri);
            StreamReader commentRes = new StreamReader(commentRequest.GetResponse().GetResponseStream());
            string commentJson = commentRes.ReadToEnd();
            JObject jobject = JObject.Parse(commentJson);
            if (jobject["data"].HasValues == false) return;
            List<JToken> medialstToken;
            if (jobject["data"][0]["subattachments"] == null)
            {
                Attachment a = new Attachment();
                a.src = jobject["data"][0]["media"]["image"]["src"].ToString();
                a.decription = GetDescription(a.src);
                userPost.listAttachment.Add(a);
            }
            else
            {
                medialstToken = jobject["data"][0]["subattachments"]["data"].ToList();
                foreach (JToken token in medialstToken)
                {
                    Attachment at = new Attachment();
                    at.src = token["media"]["image"]["src"].ToString();
                    at.decription = GetDescription(at.src);
                    userPost.listAttachment.Add(at);
                }
            }
        }

        private string readPost(string accessToken, string id)
        {
            string result = "";
            Uri uri = new Uri("https://graph.facebook.com/" + id + "/feed?access_token=" + accessToken);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            StreamReader res = new StreamReader(request.GetResponse().GetResponseStream());
            string jsonres = res.ReadToEnd();
            // Deserialize and convert the JSON object to the Facebook.User object type
            JavaScriptSerializer sr = new JavaScriptSerializer();
            //FacebookClient fbClient = new FacebookClient();
            //fbClient.Get("/" + id + "/feed&access_token=" + accessToken);
            JObject json = JObject.Parse(jsonres);
            List<JToken> lstToken = json["data"].ToList();
            List<UserPosts> lstPost = new List<UserPosts>();
            foreach (JToken t in lstToken)
            {
                UserPosts p = sr.Deserialize<UserPosts>(t.ToString());
                lstPost.Add(p);
            }
            // loop through userPost
            foreach (UserPosts userPost in lstPost)
            {
                // get comment info
                Uri commentUri = new Uri("https://graph.facebook.com/" + userPost.id + "/comments?access_token=" + accessToken);
                HttpWebRequest commentRequest = (HttpWebRequest)HttpWebRequest.Create(commentUri);
                StreamReader commentRes = new StreamReader(commentRequest.GetResponse().GetResponseStream());
                string commentJson = commentRes.ReadToEnd();
                JObject jobject = JObject.Parse(commentJson);
                List<JToken> commentlstToken = jobject["data"].ToList();

                List<UserComment> lstComment = new List<UserComment>();
                foreach (JToken token in commentlstToken)
                {
                    UserComment userComment = sr.Deserialize<UserComment>(token.ToString());
                    lstComment.Add(userComment);
                }
                userPost.comments = lstComment;

                // get likes info
                Uri likesUri = new Uri("https://graph.facebook.com/" + userPost.id + "/likes?access_token=" + accessToken);
                HttpWebRequest likesRequest = (HttpWebRequest)HttpWebRequest.Create(likesUri);
                StreamReader likesRes = new StreamReader(likesRequest.GetResponse().GetResponseStream());
                string likesJson = likesRes.ReadToEnd();
                JObject likesObject = JObject.Parse(likesJson);
                int likesCount = likesObject["data"].ToList().Count;
                userPost.likesCount = likesCount;

                // get post details
                getDetails(userPost, accessToken, id);
                getAttachment(userPost, accessToken);
                result += userPost.getTextToSpeak() + " ";
            }

            return result;
        }

        #endregion
    }
}
