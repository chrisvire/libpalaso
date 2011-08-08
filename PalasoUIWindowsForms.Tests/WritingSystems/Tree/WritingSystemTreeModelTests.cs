﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using Palaso.UI.WindowsForms.WritingSystems;
using Palaso.UI.WindowsForms.WritingSystems.WSTree;
using Palaso.WritingSystems;

namespace PalasoUIWindowsForms.Tests.WritingSystems.Tree
{
	[TestFixture]
	public class WritingSystemTreeModelTests
	{
		private IWritingSystemRepository _writingSystemRepository;
		private WritingSystemTreeModel _model;
		private Mock<WritingSystemSetupModel> _mockSetupModel;

		[SetUp]
		public void Setup()
		{
			_writingSystemRepository = new LdmlInFolderWritingSystemRepository();
			_mockSetupModel = new Mock<WritingSystemSetupModel>(_writingSystemRepository);
			SetDefinitionsInStore(new WritingSystemDefinition[] { });
			_model = new WritingSystemTreeModel(_mockSetupModel.Object);
			_model.Suggestor = new WritingSystemSuggestor
			{
				SuggestIpa = false,
				SuggestOther = false,
				SuggestDialects = false,
				SuggestVoice = false,
				OtherKnownWritingSystems = null
			};
		}

		[Test]
		public void GetTopLevelItems_OtherKnownWritingSystemsIsNull_Ok()
		{
			SetDefinitionsInStore(new WritingSystemDefinition[] { });
			_model.Suggestor.OtherKnownWritingSystems = null;
			 AssertTreeNodeLabels("Add Language");
		}


		[Test]
		public void GetTopLevelItems_StoreIsEmptyButOtherLanguagesAreAvailable_GivesOtherLanguageChoiceHeader()
		{
			SetDefinitionsInStore(new WritingSystemDefinition[]{});
			_model.Suggestor.OtherKnownWritingSystems = new List<WritingSystemDefinition>(new[] { new WritingSystemDefinition("en") });
			AssertTreeNodeLabels("Add Language", "", "Other Languages", "+Add English");
		}

		/// <summary>
		/// THe point here is, don't show a language under other, once it has been added to the collection
		/// </summary>
		[Test]
		public void GetTopLevelItems_StoreAlreadyHasAllOsLanguages_DoesNotOfferToCreateItAgain()
		{
			var en = new WritingSystemDefinition("en");
			var de = new WritingSystemDefinition("de");
			var green = new WritingSystemDefinition("fr");
			_model.Suggestor.OtherKnownWritingSystems = new[]{de, green};
			SetDefinitionsInStore(new[] { en, de });
			AssertTreeNodeLabels("English", "German","", "Add Language","", "Other Languages", "+Add French" /*notice, no de*/);

	  }


		[Test]
		public void GetTopLevelItems_StoreAlreadyHasAllOsLanguages_DoesNotGiveLanguageChoiceHeader()
		{
			var en = new WritingSystemDefinition("en");
			var de = new WritingSystemDefinition("de");
			_model.Suggestor.OtherKnownWritingSystems = new[] { de };
			SetDefinitionsInStore(new[] { en, de });
			AssertTreeNodeLabels("English", "German", "", "Add Language");

		}

		private void AssertTreeNodeLabels(params string[] names)
		{
			var items = _model.GetTreeItems().ToArray();
			int childIndex = 0;
			for (int i = 0, x=-1; i < names.Count(); i++)
			{
				string itemText;
				if (!string.IsNullOrEmpty(names[i]) && names[i].Substring(0, 1) == "+")
				{
					var child = items[x].Children[childIndex];
					itemText = "+"+child.Text;
					++childIndex;
				}
				else
				{
					//if we aren't looking at a child node, move to the next top level node
					++x;
					itemText = items[x].Text;
					childIndex = 0;
				}
				if (names[i] != itemText)
				{
					PrintExpectationsVsActual(names, items);
				}
				Assert.AreEqual(names[i], itemText);
				int total=0;
				foreach (var item in items)
				{
					++total;
					total+=item.Children.Count();
				}
				if(names.Count()!=total)
					PrintExpectationsVsActual(names, items);
				Assert.AreEqual(names.Count(), total,"the actual nodes exceded the number of expected ones");
			}
		}

		private void PrintExpectationsVsActual(string[] names, WritingSystemTreeItem[] items)
		{
			Console.Write("exp: ");
			names.ToList().ForEach(c => Console.Write(c + ", "));
			Console.WriteLine();
			Console.Write("got: ");
			foreach (var item in items)
			{
				Console.Write(item.Text+", ");
				item.Children.ForEach(c=>Console.Write(c.Text+", "));
			}
		}

		[Test]
		public void GetTopLevelItems_TwoLanguagesInStore_GivesBoth()
		{
			var xyz = new WritingSystemDefinition("en");
			var abc = new WritingSystemDefinition("de");
			SetDefinitionsInStore(new[] { abc, xyz });
			AssertTreeNodeLabels( "German", "English","", "Add Language");
		}

		private void SetDefinitionsInStore(IEnumerable<WritingSystemDefinition> defs)
		{
			_mockSetupModel.SetupGet(x => x.WritingSystemDefinitions).Returns(new List<WritingSystemDefinition>(defs));
		}

		[Test]
		public void GetTopLevelItems_OneLanguageIsChildOfAnother_GivesParentOnly()
		{
			var etr = new WritingSystemDefinition("etr", string.Empty, string.Empty, string.Empty, "edo", false);
			var etrIpa = new WritingSystemDefinition("etr", string.Empty, string.Empty,"fonipa", "edo", false);
			SetDefinitionsInStore(new[] { etr,etrIpa });
			_model.Suggestor.SuggestIpa=true;
			AssertTreeNodeLabels("Edolo", "+Edolo (IPA)", "", "Add Language");
		}


		/// <summary>
		/// related to http://projects.palaso.org/issues/show/482
		/// </summary>
		[Test]
		public void GetTopLevelItems_ThreeVariantsAreSyblings_ListsAllUnderGroupHeading()
		{
			var thai = new WritingSystemDefinition("bzi", "Thai", string.Empty, string.Empty, "bt", false);
			var my = new WritingSystemDefinition("bzi", "Mymr", string.Empty, string.Empty, "bm", false);
			var latin = new WritingSystemDefinition("bzi", "Latn", string.Empty, string.Empty, "bl", false);
			SetDefinitionsInStore(new[] { thai, my, latin });
			AssertTreeNodeLabels("Bisu", "+Bisu (Thai)", "+Bisu (Mymr)", "+Bisu (Latn)", "", "Add Language");
		}

		/// <summary>
		/// Other details of this behavior are tested in the class used as the suggestor
		/// </summary>
		[Test]
		public void GetTopLevelItems_UsesSuggestor()
		{
			var etr = new WritingSystemDefinition("etr", string.Empty, string.Empty, string.Empty, "edo", false);
			SetDefinitionsInStore(new WritingSystemDefinition[] {etr });
			_model.Suggestor.SuggestIpa = true;
			AssertTreeNodeLabels("Edolo", "+Add IPA writing system for Edolo", "", "Add Language");
		}

		[Test]
		public void ClickAddLanguage_AddNewCalledOnSetupModel()
		{
			/* the tree would look like this:
			  Add Language  <-- we're clicking this one
			*/
			var items = _model.GetTreeItems();
			items.First().Clicked();
			_mockSetupModel.Setup(m => m.AddNew());
			_mockSetupModel.Verify(m => m.AddNew(), "Should have called the AddNew method on the setup model");
		}

		[Test]
		public void ClickAddPredifinedLanguage_AddNewCalledOnSetupModel()
		{
			/* the tree would look like this:
				Add Language
				Other Languages
				  Add xyz     <-- we're clicking this one
			 */

			var def = new WritingSystemDefinition("en");
			_model.Suggestor.OtherKnownWritingSystems = new List<WritingSystemDefinition>(new[] { def });
			var items = _model.GetTreeItems();
			_mockSetupModel.Setup(m => m.AddPredefinedDefinition(def));


			items.Last().Children.First().Clicked();
			_mockSetupModel.Verify(m => m.AddPredefinedDefinition(def));
		}

		[Test]
		public void ClickExistingLanguage_SelectCalledOnSetupModel()
		{
			/* the tree would look like this:
				xyz               <-- we're clicking this one
				Add Language
			 */

			var def = new WritingSystemDefinition("en");
			SetDefinitionsInStore(new WritingSystemDefinition[] { def });
			var items = _model.GetTreeItems();
			_mockSetupModel.Setup(m => m.SetCurrentDefinition(def));


			items.First().Clicked();
			_mockSetupModel.Verify(m => m.SetCurrentDefinition(def));
		}
	}


}