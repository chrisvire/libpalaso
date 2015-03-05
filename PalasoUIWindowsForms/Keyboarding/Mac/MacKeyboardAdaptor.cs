#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using MonoMac.Foundation;
using Palaso.UI.WindowsForms.Keyboarding.Interfaces;
using Palaso.UI.WindowsForms.Keyboarding.InternalInterfaces;
using Palaso.WritingSystems;

namespace Palaso.UI.WindowsForms.Keyboarding.Mac
{
	internal class MacKeyboardAdaptor : IKeyboardAdaptor
	{
		protected List<IKeyboardErrorDescription> m_BadKeyboards;
		protected MonoMac.AppKit.NSTextInputContext m_Context;

		public MacKeyboardAdaptor()
		{
		}

		protected void InitKeyboards()
		{
			if (m_BadKeyboards != null)
			{
				return;
			}

			ReinitKeyboards();
		}

		private void ReinitKeyboards()
		{
			m_BadKeyboards = new List<IKeyboardErrorDescription>();
			m_Context = new MonoMac.AppKit.NSTextInputContext();
			var sources = NSArray.FromArray<NSString>(m_Context.KeyboardInputSources);
			foreach (var source in sources.Select(kis => kis.ToString()))
			{
				var localizedName = MonoMac.AppKit.NSTextInputContext.LocalizedNameForInputSource(source).ToString();
				var keyboard = new MacKeyboardDescription(source, localizedName, this);
				KeyboardController.Manager.RegisterKeyboard(keyboard);
			}
		}
		public void Initialize()
		{
			InitKeyboards();
		}

		public void UpdateAvailableKeyboards()
		{
			ReinitKeyboards();
		}

		public void Close()
		{
			m_Context = null;
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards { get; private set; }
		public bool ActivateKeyboard(IKeyboardDefinition keyboard)
		{
			Debug.Assert(keyboard is KeyboardDescription);
			Debug.Assert(((KeyboardDescription)keyboard).Engine == this);
			Debug.Assert(keyboard is MacKeyboardDescription);
			var cocoaKeyboard = keyboard as MacKeyboardDescription;
			m_Context.SelectedKeyboardInputSource = cocoaKeyboard.InputSource;

			return true;
		}

		public void DeactivateKeyboard(IKeyboardDefinition keyboard)
		{
		}

		public IKeyboardDefinition GetKeyboardForInputLanguage(IInputLanguage inputLanguage)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition CreateKeyboardDefinition(string layout, string locale)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition ActiveKeyboard
		{
			get
			{
				var InputSource = m_Context.SelectedKeyboardInputSource;
				return Keyboard.Controller.AllAvailableKeyboards.OfType<MacKeyboardDescription>()
					.FirstOrDefault(macKeybd => macKeybd.InputSource == InputSource);
			}

		}

		public IKeyboardDefinition DefaultKeyboard { get; private set; }
		public KeyboardType Type { get { return KeyboardType.System;}}
	}
}
#endif

