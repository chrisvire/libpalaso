using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIL.WritingSystems.WindowsForms.WSTree
{
	public interface IWritingSystemDefinitionSuggestion
	{
		string Label { get; }
		WritingSystemDefinition ShowDialogIfNeededAndGetDefinition();
	}

	public abstract class WritingSystemSuggestion : IWritingSystemDefinitionSuggestion
	{
		public WritingSystemDefinition TemplateDefinition { get; protected set; }

		public string Label { get; protected set; }
		public abstract WritingSystemDefinition ShowDialogIfNeededAndGetDefinition();

		protected void SetLabelDetail(string detail)
		{
			Label = string.Format("{0} ({1})", TemplateDefinition.LanguageName, detail);
		}
	}

	public class VoiceSuggestion : WritingSystemSuggestion
	{
		public VoiceSuggestion(WritingSystemDefinition primary)
		{
			TemplateDefinition = primary.Clone();
			TemplateDefinition.IsVoice = true;
			SetLabelDetail("voice");
		}

		public override WritingSystemDefinition ShowDialogIfNeededAndGetDefinition()
		{
			return TemplateDefinition;
		}
		public static bool ShouldSuggest(IEnumerable<WritingSystemDefinition> existingWritingSystemsForLanguage)
		{
			return !existingWritingSystemsForLanguage.Any(def => def.IsVoice);
		}
	}

	public class DialectSuggestion : WritingSystemSuggestion
	{
		public DialectSuggestion(WritingSystemDefinition primary)
		{
			TemplateDefinition = primary.Clone();
			Label = string.Format("new dialect of {0}", TemplateDefinition.LanguageName);
		}
		public override WritingSystemDefinition ShowDialogIfNeededAndGetDefinition()
		{
			var dlg = new GetDialectNameDialog();
			if (DialogResult.OK != dlg.ShowDialog())
				return null;
			IEnumerable<VariantSubtag> variantSubtags;
			if (IetfLanguageTag.TryGetVariantSubtags(WritingSystemDefinitionVariantHelper.ValidVariantString(dlg.DialectName), out variantSubtags))
			{
				TemplateDefinition.Variants.Clear();
				foreach (VariantSubtag variantSubtag in variantSubtags)
					TemplateDefinition.Variants.Add(variantSubtag);
			}
			return TemplateDefinition;
		}
	}

	public class IpaSuggestion : WritingSystemSuggestion
	{
		/// <summary>
		/// these are ordered in terms of perference, so the last one is just the fallback
		/// </summary>
		private readonly string[] _fontsForIpa = { "arial unicode ms", "lucinda sans unicode", "doulous sil", FontFamily.GenericSansSerif.Name };

		public IpaSuggestion(WritingSystemDefinition primary)
		{
			string ipaFontName = _fontsForIpa.FirstOrDefault(FontExists);
			FontDefinition ipaFont = string.IsNullOrEmpty(ipaFontName) ? null : new FontDefinition(ipaFontName);
			TemplateDefinition = new WritingSystemDefinition
									  {
										  Language = primary.Language,
										  Region = primary.Region,
										  LanguageName = primary.LanguageName,
										  Abbreviation = "ipa",
										  DefaultFont = ipaFont,
										  DefaultFontSize = primary.DefaultFontSize,
										  IpaStatus = IpaStatusChoices.Ipa
									  };
			foreach (VariantSubtag variantSubtag in primary.Variants)
				TemplateDefinition.Variants.Add(variantSubtag);
			var ipaKeyboard = Keyboard.Controller.AllAvailableKeyboards.FirstOrDefault(k => k.Id.ToLower().Contains("ipa"));
			if (ipaKeyboard != null)
				TemplateDefinition.Keyboard = ipaKeyboard.Id;
			Label = string.Format("IPA input system for {0}", TemplateDefinition.LanguageName);
		}
		public override WritingSystemDefinition ShowDialogIfNeededAndGetDefinition()
		{
			return TemplateDefinition;
		}


		private static bool FontExists(string name)
		{
			var f = new Font(name, 12);
			return f.Name.ToLower() == name.ToLower();
		}

		public static bool ShouldSuggest(IEnumerable<WritingSystemDefinition> existingWritingSystemsForLanguage)
		{
			return existingWritingSystemsForLanguage.All(def => def.IpaStatus == IpaStatusChoices.NotIpa);
		}
	}

	public class OtherSuggestion : WritingSystemSuggestion
	{
		public OtherSuggestion(WritingSystemDefinition primary, IEnumerable<WritingSystemDefinition> exisitingWritingSystemsForLanguage)
		{
			TemplateDefinition = WritingSystemDefinition.CreateCopyWithUniqueId(primary, exisitingWritingSystemsForLanguage.Select(ws=>ws.Id));
			Label = string.Format("other input system for {0}", TemplateDefinition.LanguageName);
		}
		public override WritingSystemDefinition ShowDialogIfNeededAndGetDefinition()
		{
			return TemplateDefinition;
		}
	}

	/// <summary>
	/// used to suggest languages not yet represented. Contrast with the other classes here,
	/// which are use to suggest new writing systems based on already in-use languages
	/// </summary>
	public class LanguageSuggestion : WritingSystemSuggestion
	{
		public LanguageSuggestion(WritingSystemDefinition definition)
		{
			TemplateDefinition = definition;
			Label = string.Format(TemplateDefinition.ListLabel);
		}
		public override WritingSystemDefinition ShowDialogIfNeededAndGetDefinition()
		{
			return TemplateDefinition;
		}
	}
}