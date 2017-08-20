using System;
using System.Collections.Generic;
using System.Text;

namespace DozensAPI.Test
{
    internal class Dozens_TestAdaptor : Dozens
    {
        public Dozens_TestAdaptor() : base()
        {
        }

        public Dozens_TestAdaptor(string dozensUserId, string apiKey) : base(dozensUserId, apiKey)
        {
        }

        public new void Auth()
        {
            base.Auth();
        }
    }
}
