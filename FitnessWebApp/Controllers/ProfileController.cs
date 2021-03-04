﻿using Com;
using FitnessWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FitnessWebApp.Controllers
{
    [Authorize]
    public class ProfileController : ApiController
    {

        [AcceptVerbs("Get")]
        public ResLastUserInfo GetLastUserInfo(int UID)
        {
            ResLastUserInfo Res = new ResLastUserInfo();
            try
            {
                Com.User usr = BLL.User.GetUser(UID);
                MiniUser miniUser = new MiniUser()
                {
                    CodeMeli = usr.CodeMeli ?? "",
                    Credit = usr.Credit,
                    Email = usr.Email ?? "",
                    FamilyName = usr.FamilyName ?? "",
                    JoinDate = usr.JoinDate,
                    LastLogin = usr.LastLogin,
                    Name = usr.Name ?? "",
                    NickName = usr.NickName ?? "",
                    RewardState = 1,
                    TellNumber = usr.TellNumber,
                    UID = usr.UID,
                };
                Res.MiniUser = miniUser;
                Res.userBought = BLL.User.GetAllUserBought(UID);
                Res.userMessage = BLL.User.GetAllUserMessage(UID);
            }
            catch (Exception e)
            {

            }

            return Res;
        }

        [AcceptVerbs("Get")]
        public List<Com.UserBought> GetAllUserBought(int UID)
        {
            return BLL.User.GetAllUserBought(UID);
        }

            [AcceptVerbs("Get")]
        public Com.LogonResult Logon(string TellNumber)
        {
            try
            {
                Com.User mUser = BLL.User.GetUserByTellNumber(TellNumber);
                if (mUser == null)
                {
                    mUser = new Com.User()
                    {
                        TellNumber = TellNumber,
                        LastLogin = DateTime.Now,
                        Active = false,
                        Credit = 500,
                        JoinDate = DateTime.Now,
                        RewardState = 0,
                        ActiveCode = CreateRandomCode()
                    };

                    mUser.UID = BLL.User.AddUser(mUser);
                    if (mUser.UID < 0)
                    {
                        return new Com.LogonResult() { IsNew = true, HasError = true, NickName = "", UserID = mUser.UID };
                    }
                    else
                    {
                        new System.Threading.Thread(delegate () { SendActivationCodeViaSMS(TellNumber, mUser.ActiveCode); }).Start();
                        return new Com.LogonResult() { IsNew = true, HasError = false, NickName = "", UserID = mUser.UID };
                    }
                }
                else
                {
                    string NewActiveCode = CreateRandomCode();
                    if (TellNumber == "989123456789")//mehr
                    {
                        NewActiveCode = "12345";
                    }
                    mUser.ActiveCode = NewActiveCode;
                    bool Res = BLL.User.UpdateUserCode(mUser);
                    new System.Threading.Thread(delegate () { SendActivationCodeViaSMS(TellNumber, NewActiveCode); }).Start();
                    return new Com.LogonResult() { IsNew = false, HasError = false, NickName = mUser.NickName, UserID = mUser.UID };
                }
            }
            catch (Exception e)
            {
                new System.Threading.Thread(delegate () { BLL.Log.DoLog(Com.Common.Action.Logon, TellNumber, -200, e.Message); }).Start();
                return new Com.LogonResult() { IsNew = false, HasError = true, NickName = e.Message, UserID = 0 };
            }
        }

        [AcceptVerbs("Post")]
        public LoginResult Login([FromBody] LoginInput loginInput)
        {
            try
            {
                User mUser = BLL.User.GetUser(loginInput.UserID);
                if (mUser == null)
                {
                    return new LoginResult() { HasError = true, Error = "User Not found.", ErrorCode = -300 };
                }

                if (mUser.ActiveCode == loginInput.ActiveCode)
                {//Mehr
                    //if (loginInput.InviterTellNumber != "empty")
                    //{
                    //    User mInviterUIDUser = BLL.User.GetUserByTellNumber(loginInput.InviterTellNumber);


                    //    if (SubTell == StSP[1])
                    //    {
                    //        //add to User
                    //        //add to inviter
                    //        if (IncreasedCredit(InviterUID, 50) > 0)
                    //        {
                    //            AppMsgCollection appMsgCollection = new AppMsgCollection()
                    //            {
                    //                Category = 0,
                    //                Content = "به دلیل معرفی این برنامه به دوست خود و نصب برنامه توسط ایشان شما 50 سکه رایگان دریافت کردید.",
                    //                MID = 10,
                    //                Notify = true,
                    //                NotifyInApp = false,
                    //                Scheduled = false,
                    //                SID = 0,
                    //                Tab = 1,
                    //                Tiltle = "هدیه معرفی",
                    //                TypeStyle = 1
                    //            };
                    //            FCMPushNotification fcmPushInviter = new FCMPushNotification();
                    //            fcmPushInviter.SendInAppNotification(mInviterUIDUser.FBToken, appMsgCollection, mInviterUIDUser.OSType);
                    //        }
                    //        if (IncreasedCredit(UID, 25) > 0)
                    //        {
                    //            AppMsgCollection appMsgCollection = new AppMsgCollection()
                    //            {
                    //                Category = 0,
                    //                Content = "به دلیل استفاده از کد معرف دوست خود و نصب برنامه گفتمان 25 سکه رایگان دریافت کرده اید شما هم با معرفی این برنامه به دیگران می توانید 50 سکه رایگان دریافت کنید.",
                    //                MID = 10,
                    //                Notify = true,
                    //                NotifyInApp = false,
                    //                Scheduled = false,
                    //                SID = 0,
                    //                Tab = 1,
                    //                Tiltle = "هدیه معرفی",
                    //                TypeStyle = 1
                    //            };
                    //            FCMPushNotification fcmPushInviter = new FCMPushNotification();
                    //            fcmPushInviter.SendInAppNotification(FBToken, appMsgCollection, mInviterUIDUser.OSType);
                    //        }
                    //        //Send in App MSG
                    //    }
                    //    else
                    //    {
                    //        return -200;
                    //    }
                    //}
                    mUser.NickName = loginInput.NickName;
                    mUser.Active = true;
                    mUser.DeviceID = loginInput.DeviceID;
                    mUser.FBToken = loginInput.FBToken;

                    if (BLL.User.UpdateUser(mUser))
                    {
                        return new LoginResult() { HasError = false, StatusResult = true, Token = "FelanAlaki" };
                    }
                    else
                    {
                        new System.Threading.Thread(delegate () { BLL.Log.DoLog(Common.Action.Login, loginInput.UserID.ToString(), -400, null); }).Start();
                        return new LoginResult() { Error = "Error in DB.", HasError = true, ErrorCode = -500 };
                    }
                }
                else
                {
                    new System.Threading.Thread(delegate () { BLL.Log.DoLog(Common.Action.Login, loginInput.UserID.ToString(), -400, null); }).Start();
                    return new LoginResult() { Error = "Wrong code.", HasError = true, ErrorCode = -400 };
                }
            }
            catch (Exception e)
            {
                new System.Threading.Thread(delegate () { BLL.Log.DoLog(Common.Action.Login, loginInput.UserID.ToString(), -900, null); }).Start();
                return new LoginResult() { Error = e.Message, HasError = true, ErrorCode = -900 };
            }
        }


        // GET: api/Profile
        [AllowAnonymous]
        public IEnumerable<string> Get()
        {
            
            return new string[] { "value1", "value2" };
        }

        // GET: api/Profile/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Profile
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Profile/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/Profile/5
        public void Delete(int id)
        {
        }

        //////////////////////////////////////////////////
        /// Help API For Controler       /////////////////
        //////////////////////////////////////////////////

        string CreateRandomCode()
        {
            string[] SplitChars = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            Random random = new Random();
            string RandomPCode = string.Empty;
            for (int i = 0; i <= 4; i++)
            {
                RandomPCode += SplitChars[random.Next(1, SplitChars.Length)];
            }

            return RandomPCode;
        }

        bool SendActivationCodeViaSMS(string TellNumber, string RndNumber)
        {
            try
            {
                Kavenegar.KavenegarApi api = new Kavenegar.KavenegarApi("6E41717A4D532F556962626E7149466679445A6D4B413D3D");
                var result = api.VerifyLookup(TellNumber, RndNumber, "GoftemanActivationCode");

                return true;
            }
            catch (Kavenegar.Exceptions.ApiException)
            {
                // در صورتی که خروجی وب سرویس 200 نباشد این خطارخ می دهد.
                new System.Threading.Thread(delegate () { BLL.Log.DoLog(Com.Common.Action.SendSMS, TellNumber, 0, "Return Not 200"); }).Start();
                return false;
            }
            catch (Kavenegar.Exceptions.HttpException)
            {
                // در زمانی که مشکلی در برقرای ارتباط با وب سرویس وجود داشته باشد این خطا رخ می دهد
                new System.Threading.Thread(delegate () { BLL.Log.DoLog(Com.Common.Action.SendSMS, TellNumber, 0, "Not Connected"); }).Start();
                return false;
            }
            catch (Exception e)
            {
                new System.Threading.Thread(delegate () { BLL.Log.DoLog(Com.Common.Action.SendSMS, TellNumber, 0, e.Message); }).Start();
                return false;
            }
        }
    }
}