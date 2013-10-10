using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TellToAsk.Areas.Administration.Models
{
    public class AnswerView
    {
        public int Id { get; set; }

        [StringLength(500, MinimumLength = 50)]
        [Required]
        [DataType(DataType.MultilineText)]
        [AllowHtml]
        public string Text { get; set; }

        public string Reported { get; set; }
    }
}