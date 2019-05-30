?using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using Neighborly.Models;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.WebPages;
using Facebook;
using Newtonsoft.Json;
using System.Web.Security;
using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;


namespace Neighborly.Controllers
{   

   


    public class HomeController : Controller
    {
      

        public ActionResult Index()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);

           if(HttpContext.Request.Cookies["email"] == null)
            {
                return RedirectToAction("Facebook");
               
            }

            string emailCookieValue = Request.Cookies["email"].Value;
            string firstNameCookieValue = Request.Cookies["first_name"].Value;

            if (GetUserCount(emailCookieValue) < 1)
            {
                AddUser(emailCookieValue, firstNameCookieValue);
                //redirect to enter phone number page
                return RedirectToAction("CanWeTextYou", "Home");

              
            }


            //set phone cookie , fetch via sql query
            SetPhoneCookie(emailCookieValue);


           

            return View();
        }


      


        public void SetPhoneCookie(string email)
        {
            string phoneString;
            string connString = getConnString();
            string SQLCode = "select phone from NeighborlyUsers where userEmail = '" + email + "'";
            connection();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                phoneString = (string)com.ExecuteScalar();
                conn.Close();
            }

            DateTime now = DateTime.Now;
            //add phone cookie
            HttpCookie phoneCookie = new HttpCookie("phone");
            phoneCookie.Value = phoneString;
            phoneCookie.Expires = now.AddYears(3);
            this.ControllerContext.HttpContext.Response.Cookies.Add(phoneCookie);


            return;
        }


        public void SendTextSignUp()
        {
            string phoneString = Request.Cookies["phone"].Value;
            string firstNameCookieValue = Request.Cookies["first_name"].Value;
            string message = "Thanks for signing up for Neighborly " + firstNameCookieValue + "!";
            SendText(phoneString, message);
        }

        public void SendTextCreateN(string neighCode)
        {
            string phoneCookieValue = Request.Cookies["phone"].Value;
            string firstNameCookieValue = Request.Cookies["first_name"].Value;
            string neighPasswordCookie = Request.Cookies["neighPW"].Value;
            //string neighNameCookie = Request.Cookies["neighName"].Value;
            
            string message3 = "Welcome to the neighborhood, " + firstNameCookieValue + "! The secret password is " + neighCode + ". Sign in here: http://neighborly.azurewebsites.net/";
            SendText(phoneCookieValue, message3);
        }


        public void SendText(string phone, string message2)
        {
            // Find your Account Sid and Token at twilio.com/console
            const string accountSid = "xxxx";
            const string authToken = "xxxx";

            TwilioClient.Init(accountSid, authToken);
            var message = MessageResource.Create(
                body: message2,
                from: new Twilio.Types.PhoneNumber("+1619nnnnnnn"),
                to: new Twilio.Types.PhoneNumber("+1"+phone)
            );
           
        }


        private Uri RedirectUri
        {
            get
            {
                var uriBuilder = new UriBuilder(Request.Url);
                uriBuilder.Query = null;
                uriBuilder.Fragment = null;
                uriBuilder.Path = Url.Action("facebookCallback");
                return uriBuilder.Uri;
            }
        }

        [AllowAnonymous]
        [HandleError]
        [RequiresSSL]
        public ActionResult Facebook()
        {
            var fb = new FacebookClient();
            var loginurl = fb.GetLoginUrl(new
            {
               // client_id = "xxx",     //test app
               //   client_secret = "xxx",     //test app
               client_id = "xxx",     //regular app
               client_secret = "xxx",   //regular app
                redirect_uri = RedirectUri.AbsoluteUri,
                response_type = "code",
                scope = "email"
            });

            return Redirect(loginurl.AbsoluteUri);
        }

                
        public ActionResult FacebookCallback(string code)
        {      

                var fb = new FacebookClient();
                dynamic result = fb.Post("oauth/access_token", new
                {
                  // client_id = "xxx",        //test app
                  //  client_secret = "xxx",    //test app
                      client_id = "xxx",   //regular app
                      client_secret = "xxx",   //regular app
                    redirect_uri = RedirectUri.AbsoluteUri,
                    code = code

                });
                var accessToken = result.access_token;
                Session["AccessToken"] = accessToken;
                fb.AccessToken = accessToken;
                dynamic me = fb.Get("me?fields=link,first_name,currency,last_name,email,gender,locale,timezone,verified,picture,age_range");
                string email = me.email;
                string firstName = me.first_name;
                string lastname = me.last_name;
                string picture = me.picture.data.url;
                FormsAuthentication.SetAuthCookie(email, false);
                FormsAuthentication.SetAuthCookie(firstName, false);

                DateTime now = DateTime.Now;

                //add email cookie
                HttpCookie emailCookie = new HttpCookie("email");
                emailCookie.Value = email;
                emailCookie.Expires = now.AddYears(3);
                this.ControllerContext.HttpContext.Response.Cookies.Add(emailCookie);

                //add firstName cookie
                HttpCookie firstNameCookie = new HttpCookie("first_name");
                firstNameCookie.Value = firstName;
                firstNameCookie.Expires = now.AddYears(3);
                this.ControllerContext.HttpContext.Response.Cookies.Add(firstNameCookie);
            
            return Redirect("Index");
        }


        private void AddUser(string email, string firstName)
        {
            string connString = getConnString();
            string SQLCode = "Insert into NeighborlyUsers (firstName, userEmail) values('"+firstName+"','"+email+"')";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();
            }
        }

        private int GetUserCount(string email)
        {
            int userCount;
            string connString = getConnString();
            string SQLCode = "select Count(*) from NeighborlyUsers where userEmail = '"+email+"'";
            connection();
         
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                userCount = (int)com.ExecuteScalar();
                conn.Close();
            }
     
            return userCount;
        }
        

        public ActionResult CanWeTextYou()
        {
         
            return View();
        }

        public ActionResult JoinANeighborhood()
        {

            return View();
        }


        [HttpPost]
        public ActionResult JoinANeighborhood(string NeighborhoodCode)
        {
            int neighCount;
            //check to make sure the code is valid
            string connString = getConnString();
            string SQLCode = "select Count(*) from Neighborhoods where NeighborhoodCode = '" + NeighborhoodCode + "'";
            connection();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                neighCount = (int)com.ExecuteScalar();
                conn.Close();
            }

            if(neighCount < 1)
            {
                //tell user he entered in wrong code
                return JavaScript("window.alert('you entered in an invalid code!'); document.getElementById('join').disabled = false;");
               
            }


            //post to NeighborhoodUsers
            connection();
            string email = Request.Cookies["email"].Value;
            string SQLCode2 = "Insert into NeighborhoodUsers (NeighborhoodCode, UserEmail) " +
                             "values('" + NeighborhoodCode + "','" + email + "')";

            using (SqlConnection conn = new SqlConnection(connString))
            {

                SqlCommand com = new SqlCommand(SQLCode2, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();

            }


            //get neighborhood name
            string SQLCode3 = "select neighborHoodName from Neighborhoods where NeighborhoodCode = '" + NeighborhoodCode + "'";
            connection();
            string neighName = "";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode3, conn);
                conn.Open();
                neighName = (string)com.ExecuteScalar();
                conn.Close();
            }



            //return Redirect("http://neighborly.azurewebsites.net/Home/ImGoingToTheStore");

            return JavaScript("window.alert('welcome to "+neighName+ "!'); window.location.href = ' / '");
           
        }


        [HttpPost]
        public ActionResult CanWeTextYou(string phone)
        {
            string emailCookieValue = Request.Cookies["email"].Value;
            string connString = getConnString();
            string SQLCode = "Update NeighborlyUsers set phone = '" + phone + "' where userEmail = '" + emailCookieValue + "'";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();
            }

            DateTime now = DateTime.Now;

            HttpCookie phoneCookie = new HttpCookie("phone");
            phoneCookie.Value = phone;
            phoneCookie.Expires = now.AddYears(3);
            this.ControllerContext.HttpContext.Response.Cookies.Add(phoneCookie);

            return RedirectToAction("Index", "Home");
        }


        public ActionResult NeedSomething()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);          
            return View();
        }

        
        public ActionResult GoToANeighborhood()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);

            NavigationViewModel viewModel;

            viewModel = new NavigationViewModel();


            DataTable datatableSQL2 = new DataTable();
            string constr = getConnString();
            string SQLCode = @"select neigh.NeighborHoodName from NeighborlyUsers n
                                left join NeighborhoodUsers nu on n.userEmail = nu.userEmail
                                left join Neighborhoods neigh on nu.NeighborhoodCode = neigh.NeighborhoodCode
                                where n.userEmail like '%dahlk%'";


            using (SqlConnection conn = new SqlConnection(constr))
            {
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand(SQLCode, conn);
                conn.Open();
                adapter.Fill(datatableSQL2);
                conn.Close();
            }

            //convert dt to iEnumerable

            viewModel.Neighborhoods = ConvertDT(datatableSQL2);

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult GoToANeighborhood(string NeighborhoodName)
        {
            

            if (Request.Cookies["defaultNeigh"] != null)
            {
                var c = new HttpCookie("defaultNeigh");
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }

          

            int neighCode = GetNeighCode(NeighborhoodName);
            SetDefaultNeigh(neighCode, NeighborhoodName);
            
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);
            return RedirectToAction("Index", "Home");
        }


        private int GetNeighCode(string neighName)
        {

            string email = Request.Cookies["email"].Value;

            int neighCode;
            string connString = getConnString();
            string SQLCode = @"select distinct nu.NeighborhoodCode from Neighborhoods n 
                                left join NeighborhoodUsers nu on n.NeighborhoodCode = nu.NeighborhoodCode
                                where n.NeighborhoodName = '"+neighName+"' and nu.userEmail = '"+email+"'";
            connection();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                neighCode = (int)com.ExecuteScalar();
                conn.Close();
            }


            
            return neighCode;
        }


        private void SetDefaultNeigh(int neighCode, string NeighborhoodName)
        {


            string email = Request.Cookies["email"].Value;
            string connString = getConnString();
            string SQLCode = "Update NeighborlyUsers set currentNeighCode = '" + neighCode + "' where userEmail = '" + email + "'";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();
            }


            //add defailt neigh cookie
            DateTime now = DateTime.Now;
            HttpCookie defaultNeigh = new HttpCookie("defaultNeigh");
            defaultNeigh.Value = NeighborhoodName;
            defaultNeigh.Expires = now.AddYears(3);
            this.ControllerContext.HttpContext.Response.Cookies.Add(defaultNeigh);

            return;

        }



        private IEnumerable<Neighborhood> ConvertDT(DataTable dataTable)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                yield return new Neighborhood
                {
                    NeighborhoodName = row[0].ToString()
                };
            }
        }

        public ActionResult CreateANeighborhood()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);
            return View();
        }

        private string getConnString()
        {
            string connString = "Server = tcp:jadsolutions.database.windows.net,1433; Initial Catalog = Clients; Persist Security Info = False; User ID = xxxx; Password = xxxx; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = True; Connection Timeout = 30";
            return connString;
        }


        public ActionResult ImGoingToTheStore()
        {
            HttpContext.SetOverriddenBrowser(BrowserOverride.Desktop);

            DataTable datatableSQL = new DataTable();

            string constr = getConnString();

            string SQLCode = "select Name as 'neighbor', Description as 'requests', ExpireTime as 'expires' from Requests ORDER BY Expiration ASC";

            using (SqlConnection conn = new SqlConnection(constr))
            {
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand(SQLCode, conn);
                conn.Open();
                adapter.Fill(datatableSQL);
                conn.Close();
            }
            
            return View(datatableSQL);          
        }
        
        private SqlConnection con;

        //Post method to add details    
        [HttpPost]
        public ActionResult NeedSomething (Request obj)
        {
            TimeZoneInfo.ClearCachedData();

            int expHours = Convert.ToInt32(Regex.Match(obj.Expiration.ToString(), @"\d+").Value);
            DateTime now = DateTime.Now;
            DateTime expTime = now.AddHours(expHours);

            connection();
            SqlCommand com = new SqlCommand("AddRequest", con);
            com.CommandType = CommandType.StoredProcedure;
            com.Parameters.AddWithValue("@Name", obj.Name);
            com.Parameters.AddWithValue("@Category", obj.Category);
            com.Parameters.AddWithValue("@Description", obj.Description);
            com.Parameters.AddWithValue("@Expiration", obj.Expiration);
            com.Parameters.AddWithValue("@Status", "pending");
            com.Parameters.AddWithValue("@ExpireTime", expTime);
            com.Parameters.AddWithValue("@NeighborhoodID", "2");

            con.Open();
            com.ExecuteNonQuery();
            con.Close();

            return View();
        }

        //To Handle connection related activities    
        private void connection()
        {
            string constr = "Server = tcp:xxx; Initial Catalog = Clients; Persist Security Info = False; User ID = xxx; Password = xxxx; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = True; Connection Timeout = 30";
            con = new SqlConnection(constr);
        }

        //Post method to add details    
        [HttpPost]
        public ActionResult CreateANeighborhood(Neighborhood obj)
        {
            DateTime now = DateTime.Now;
            connection();
            string connString = getConnString();
            string SQLCode = "Insert into Neighborhoods (NeighborhoodName, NeighborhoodCategory, NeighborhoodCode) " +
                             "values('" + obj.NeighborhoodName + "','" + obj.NeighborhoodCategory + "','"+ obj.NeighborhoodCode +"')";

            using (SqlConnection conn = new SqlConnection(connString))
            {

                SqlCommand com = new SqlCommand(SQLCode, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();

            }

            //add record to linking table

            //  add firstName cookie
            HttpCookie codeCookie = new HttpCookie("neighPW");
            codeCookie.Value = obj.NeighborhoodCode;
            codeCookie.Expires = now.AddYears(3);
            this.ControllerContext.HttpContext.Response.Cookies.Add(codeCookie);

            string emailCookieValue = Request.Cookies["email"].Value;         
            string firstNameCookieValue = Request.Cookies["first_name"].Value;

            string SQLCodeLink = "Insert into NeighborhoodUsers (NeighborhoodCode, userEmail) " +
                            "values('" + obj.NeighborhoodCode + "','" + emailCookieValue + "')";

            using (SqlConnection conn = new SqlConnection(connString))
            {

                SqlCommand com = new SqlCommand(SQLCodeLink, conn);
                conn.Open();
                com.ExecuteNonQuery();
                conn.Close();

            }

            int neighCode = Convert.ToInt32(obj.NeighborhoodCode);

            SetDefaultNeigh(neighCode, obj.NeighborhoodName);


            SendTextCreateN(obj.NeighborhoodCode);

            return RedirectToAction("Index", "Home");
        }
    }

    public class RequiresSSL : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpRequestBase req = filterContext.HttpContext.Request;
            HttpResponseBase res = filterContext.HttpContext.Response;

            //Check if we're secure or not and if we're on the local box
            if (!req.IsSecureConnection && !req.IsLocal)
            {
                var builder = new UriBuilder(req.Url)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = 443
                };
                res.Redirect(builder.Uri.ToString());
            }
            base.OnActionExecuting(filterContext);
        }
    }

    
}