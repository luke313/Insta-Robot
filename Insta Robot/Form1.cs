using Insta_Robot.Models;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Insta_Robot
{
    public partial class Form1 : Form
    {
        private static IInstaApi InstaApi;
        InstDbEntities db = new InstDbEntities();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {

            await Loggin();
        }

        private async Task<bool> Loggin()
        {
            var userSession = new UserSessionData
            {
                UserName = "UserName",
                Password = "Password"
            };

            var delay = RequestDelay.FromSeconds(2, 2);

            InstaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.All))
                .SetRequestDelay(delay)
                .Build();


            const string stateFile = "state.bin";
            try
            {
                if (File.Exists(stateFile))
                {

                    //var mesage = MessageBox.Show("Loading state from file\n");


                    using (var fs = File.OpenRead(stateFile))
                    {
                        InstaApi.LoadStateDataFromStream(fs);
                        // in .net core or uwp apps don't use LoadStateDataFromStream
                        // use this one:
                        // _instaApi.LoadStateDataFromString(new StreamReader(fs).ReadToEnd());
                        // you should pass json string as parameter to this function.
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }


            if (!InstaApi.IsUserAuthenticated)
            {
                // login
                //textBox1.Text += $"Logging in as {userSession.UserName}";
                delay.Disable();
                var logInResult = await InstaApi.LoginAsync();
                delay.Enable();
                if (!logInResult.Succeeded)
                {
                    //textBox1.Text += $"Unable to login: {logInResult.Info.Message}";
                    return false;
                }
            }
            var state = InstaApi.GetStateDataAsStream();
            // in .net core or uwp apps don't use GetStateDataAsStream.
            // use this one:
            // var state = _instaApi.GetStateDataAsString();
            // this returns you session as json string.
            using (var fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }

            lblStatus.Text = "Loggin Success!";

            return true;

        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                var followers = await InstaApi.UserProcessor.GetUserFollowersAsync(txtPage.Text, PaginationParameters.MaxPagesToLoad(100));

                if (followers.Succeeded)
                {
                    foreach (var item in followers.Value)
                    {
                        var haveUser = db.FollowedTbls.Where(p => p.UserName == item.Pk.ToString());
                        if (haveUser.Count() == 0)
                        {
                            FollowedTbl model = new FollowedTbl() { UserName = item.Pk.ToString() };

                            db.FollowedTbls.Add(model);
                        }
                    }
                    db.SaveChanges();
                    MessageBox.Show("تمام کاربران با موفقیت وارد شدن");
                }
                else
                {
                    MessageBox.Show("متاسفانه نتوانستیم لیست فالوئر هارو بگیریم!");
                }
            }
            catch (Exception ea)
            {
                db.SaveChanges();
                MessageBox.Show(ea.Message);
            }

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var list = db.FollowedTbls.Where(p => p.Followed == false).ToList();
                for (int i = 0; i < int.Parse(txtCount.Text); i++)
                {
                    var follow = await InstaApi.UserProcessor.FollowUserAsync(long.Parse(list[i].UserName));

                    if (follow.Succeeded)
                    {

                        list[i].Followed = true;


                        var model = new FollowDateTbl() { UserName = list[i].UserName, StartDate = DateTime.Now.Date };
                        db.FollowDateTbls.Add(model);
                    }
                }

                var removeList = db.FollowDateTbls.ToList();

                foreach (var item in removeList)
                {
                    if (DateTime.Now.Date.Subtract(item.StartDate.Value).Days > 7)
                    {
                        var unfollow = await InstaApi.UserProcessor.UnFollowUserAsync(long.Parse(item.UserName));
                        if (unfollow.Succeeded)
                        {
                            db.FollowDateTbls.Remove(item);
                        }
                    }
                }

                db.SaveChanges();

                MessageBox.Show("باموفقیت فالو شدند.");
            }
            catch (Exception ea)
            {
                db.SaveChanges();
                MessageBox.Show(ea.Message);
            }

            
        }
    }
}
