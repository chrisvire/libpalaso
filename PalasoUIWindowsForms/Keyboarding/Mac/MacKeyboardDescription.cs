#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Palaso.UI.WindowsForms.Keyboarding.InternalInterfaces;
using Palaso.WritingSystems;

namespace Palaso.UI.WindowsForms.Keyboarding.Mac
{
	internal class MacKeyboardDescription : KeyboardDescription
	{
		internal MacKeyboardDescription(string source, string localizedName, IKeyboardAdaptor engine) :
			base(localizedName, source, String.Empty, null, engine)
		{
		}

		public string InputSource { get { return Layout; } }
	}
}
#endif

