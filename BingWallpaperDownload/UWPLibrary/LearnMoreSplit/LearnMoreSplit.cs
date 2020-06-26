using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPLibrary.LearnMoreSplit
{
    abstract class LearnMoreSplit
    {
        protected HtmlDocument _htmlDocument;

        public virtual string Title { get; protected set; }
        public virtual string Description { get; protected set; }
        public virtual string Credit { get; protected set; }
        public virtual string Href { get; protected set; }

        public LearnMoreSplit(HtmlDocument htmlDocument)
        {
            if (htmlDocument is null)
                throw new ArgumentNullException();
            if (htmlDocument.DocumentNode is null)
                throw new ArgumentException();

            _htmlDocument = htmlDocument;
        }
   
    }
}
