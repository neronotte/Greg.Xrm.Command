using Greg.Xrm.Command.Commands.Data.RecordPayload;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload
{
	[TestClass]
	public class RecordPayloadProcessorTest
	{
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;
		private RecordPayloadProcessor _processor = null!;

		[TestInitialize]
		public void Setup()
		{
			_crmMock = new Mock<IOrganizationServiceAsync2>();
			_processor = new RecordPayloadProcessor();
		}

		[TestMethod]
		public async Task ProcessAsync_WithFileField_ShouldEmitWarningAndSkipField()
		{
			var metadata = BuildEntityMetadata("contact",
				new FileAttributeMetadata { LogicalName = "new_attachment" },
				new StringAttributeMetadata { LogicalName = "firstname" });

			var payload = new Dictionary<string, object?>
			{
				["new_attachment"] = "some-value",
				["firstname"] = "Mario"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(1, result.Warnings.Count);
			Assert.IsTrue(result.Warnings[0].Contains("new_attachment"));
			Assert.IsFalse(result.Entity.Attributes.ContainsKey("new_attachment"));
			Assert.IsTrue(result.Entity.Attributes.ContainsKey("firstname"));
		}

		[TestMethod]
		public async Task ProcessAsync_WithImageField_ShouldEmitWarningAndSkipField()
		{
			var metadata = BuildEntityMetadata("contact",
				new ImageAttributeMetadata { LogicalName = "entityimage" },
				new StringAttributeMetadata { LogicalName = "lastname" });

			var payload = new Dictionary<string, object?>
			{
				["entityimage"] = "base64data",
				["lastname"] = "Rossi"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(1, result.Warnings.Count);
			Assert.IsTrue(result.Warnings[0].Contains("entityimage"));
			Assert.IsFalse(result.Entity.Attributes.ContainsKey("entityimage"));
		}

		[TestMethod]
		public async Task ProcessAsync_WithUnknownField_ShouldAccumulateError()
		{
			var metadata = BuildEntityMetadata("contact",
				new StringAttributeMetadata { LogicalName = "firstname" });

			var payload = new Dictionary<string, object?>
			{
				["nonexistentfield"] = "value"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(1, result.Errors.Count);
			Assert.IsTrue(result.Errors[0].Contains("nonexistentfield"));
		}

		[TestMethod]
		public async Task ProcessAsync_WithFieldNotValidForCreate_ShouldEmitWarningAndSkip()
		{
			var attr = new StringAttributeMetadata { LogicalName = "createdon" };
			SetIsValidForCreate(attr, false);

			var metadata = BuildEntityMetadata("contact", attr);

			var payload = new Dictionary<string, object?>
			{
				["createdon"] = "2024-01-01"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(1, result.Warnings.Count);
			Assert.IsFalse(result.Entity.Attributes.ContainsKey("createdon"));
		}

		[TestMethod]
		public async Task ProcessAsync_WithFieldNotValidForUpdate_ShouldEmitWarningAndSkip()
		{
			var attr = new StringAttributeMetadata { LogicalName = "createdon" };
			SetIsValidForUpdate(attr, false);

			var metadata = BuildEntityMetadata("contact", attr);

			var payload = new Dictionary<string, object?>
			{
				["createdon"] = "2024-01-01"
			};

			var result = await _processor.ProcessAsync(payload, metadata, false, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(1, result.Warnings.Count);
			Assert.IsFalse(result.Entity.Attributes.ContainsKey("createdon"));
		}

		[TestMethod]
		public async Task ProcessAsync_WithMultipleErrors_ShouldAccumulateAll()
		{
			var metadata = BuildEntityMetadata("contact",
				new StringAttributeMetadata { LogicalName = "firstname" });

			var payload = new Dictionary<string, object?>
			{
				["nonexistent1"] = "value1",
				["nonexistent2"] = "value2"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(2, result.Errors.Count);
		}

		[TestMethod]
		public async Task ProcessAsync_WithNullValue_ShouldSetFieldToNull()
		{
			var attr = new StringAttributeMetadata { LogicalName = "description" };
			var metadata = BuildEntityMetadata("contact", attr);

			var payload = new Dictionary<string, object?>
			{
				["description"] = null
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.IsTrue(result.Entity.Attributes.ContainsKey("description"));
			Assert.IsNull(result.Entity["description"]);
		}

		[TestMethod]
		public async Task ProcessAsync_WithEmptyStringValue_ShouldSetFieldToNull()
		{
			var attr = new StringAttributeMetadata { LogicalName = "description" };
			var metadata = BuildEntityMetadata("contact", attr);

			var payload = new Dictionary<string, object?>
			{
				["description"] = ""
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.IsTrue(result.Entity.Attributes.ContainsKey("description"));
			Assert.IsNull(result.Entity["description"]);
		}

		[TestMethod]
		public async Task ProcessAsync_HappyPath_StringField_ShouldSetValue()
		{
			var attr = new StringAttributeMetadata { LogicalName = "firstname" };
			var metadata = BuildEntityMetadata("contact", attr);

			var payload = new Dictionary<string, object?>
			{
				["firstname"] = "Mario"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(0, result.Warnings.Count);
			Assert.AreEqual("Mario", result.Entity["firstname"]);
		}

		[TestMethod]
		public async Task ProcessAsync_HappyPath_IntegerField_ShouldSetValue()
		{
			var attr = new IntegerAttributeMetadata { LogicalName = "numberofemployees" };
			var metadata = BuildEntityMetadata("account", attr);

			var payload = new Dictionary<string, object?>
			{
				["numberofemployees"] = "500"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(0, result.Errors.Count);
			Assert.AreEqual(500, result.Entity["numberofemployees"]);
		}

		[TestMethod]
		public async Task ProcessAsync_WithConversionError_ShouldAccumulateError()
		{
			var attr = new IntegerAttributeMetadata { LogicalName = "numberofemployees" };
			var metadata = BuildEntityMetadata("account", attr);

			var payload = new Dictionary<string, object?>
			{
				["numberofemployees"] = "not-a-number"
			};

			var result = await _processor.ProcessAsync(payload, metadata, true, _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(1, result.Errors.Count);
			Assert.IsFalse(result.Entity.Attributes.ContainsKey("numberofemployees"));
		}

		#region Helpers

		private static EntityMetadata BuildEntityMetadata(string logicalName, params AttributeMetadata[] attributes)
		{
			var metadata = new EntityMetadata();
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.LogicalName))!
				.SetValue(metadata, logicalName);

			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.Attributes))!
				.SetValue(metadata, attributes);

			return metadata;
		}

		private static void SetIsValidForCreate(AttributeMetadata attr, bool value)
		{
			typeof(AttributeMetadata)
				.GetProperty(nameof(AttributeMetadata.IsValidForCreate))!
				.SetValue(attr, value);
		}

		private static void SetIsValidForUpdate(AttributeMetadata attr, bool value)
		{
			typeof(AttributeMetadata)
				.GetProperty(nameof(AttributeMetadata.IsValidForUpdate))!
				.SetValue(attr, value);
		}

		#endregion
	}
}
