﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using TellToAsk.Model;

namespace TellToAsk.Areas.Administration.Models
{
    public class QuestionModel : QuestionView
    {
        public static Expression<Func<Question, QuestionModel>> FromQuestion
        {
            get
            {
                return question => new QuestionModel
                {
                    QuestionId = question.QuestionId,
                    Title = question.Title,
                    Text = question.Text,
                    AnswersCount = question.Answers.Count,
                    Creator = question.Creator.UserName,
                    AskDate = question.DateAsked,
                    Approved = question.IsApproved == false ? "not approved" : "approved",
                    Category = question.Category.Name,
                    TargetedGender = question.TargetedGender == null ? "" : (question.TargetedGender == 0 ? "Male" : "Female"),
                    TargetedMinAge = question.TargetedMinAge,
                    TargetedMaxAge = question.TargetedMaxAge
                };
            }
        }
               
        [AllowHtml]
        [DataType(DataType.MultilineText)]
        public string Text { get; set; }

        public DateTime AskDate { get; set; }

        public string AskDateAsString 
        {
            get
            {
                return this.AskDate.ToString("yyyy-MM-dd HH:mm");
            }
        }

        public int AnswersCount { get; set; }

        public string Creator { get; set; }

        public string Category { get; set; }

        public string TargetedGender { get; set; }

        public int? TargetedMinAge { get; set; }

        public string TargetedMinAgeAsString {
            get
            {
                string result = this.TargetedMinAge == null ? "" : this.TargetedMinAge.ToString();
                return result;
            }
        }

        public int? TargetedMaxAge { get; set; }

        public string TargetedMaxAgeAsString
        {
            get
            {
                string result = this.TargetedMaxAge == null ? "" : this.TargetedMaxAge.ToString();
                return result;
            }
        }
    }
}