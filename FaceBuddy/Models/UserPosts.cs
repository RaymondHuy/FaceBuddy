using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FacebookLoginASPnetWebForms.Models
{
    public class UserPosts
    {
        public string message { get; set; }
        public string created_time { get; set; }
        public string id { get; set; }
        public int likesCount { get; set; }
        public string story { get; set; }
        public NodeDestination from { get; set; }
        public List<UserComment> comments { get; set; }
        public List<Attachment> listAttachment = new List<Attachment>();
        public UserPosts postFromStory { get; set; }

        public string getTextToSpeak()
        {
            string result = "";
            if (story == null)
            {
                result = from.name + " posted status on news feed. Its content is " + message + ". It has " + likesCount + " likes ";
                if (comments != null)
                {
                    result += "and " + comments.Count + " comments. ";
                    int maxCount = 3;
                    if (comments.Count < 3) maxCount = comments.Count;
                    foreach (UserComment com in comments)
                    {
                        result += com.from.name + " has commented " + com.message + ". ";
                    }
                }
                if (listAttachment.Count > 0)
                {
                    result += "There is " + listAttachment.Count + " attachment to this post ";
                    int index = 1;
                    foreach (Attachment at in listAttachment)
                    {
                        if (index == 1)
                        {
                            result += "The description of first image is " + at.decription + ". ";
                        }
                        else if (index == 2)
                        {
                            result += "The description of second image is " + at.decription + ". ";
                        }
                        else
                        {
                            result += "The description of third image is " + at.decription + ". ";
                        }
                    }
                }
            }
            else
            {
                result += story;
            }
            result = result.Replace(" ", "   ");
            return result;
        }
    }
}