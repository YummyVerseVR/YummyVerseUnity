using System;
using System.Collections.Generic;
using NUnit.Framework;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Editor.Tests
{
    public sealed class YummyServiceV2DomainTests
    {
        private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        [Test]
        public void GeneratedFoodItemIdRemainsOpaqueAndDoesNotRequireGuid()
        {
            var value = "order_opaque_01/not-a-guid";

            Assert.That(GeneratedFoodItemId.TryCreate(value, out var id), Is.True);
            Assert.That(id.Value, Is.EqualTo(value));
            Assert.That(id.OrderIdentity, Is.EqualTo(value));
            Assert.That(id.IsValid, Is.True);
            Assert.That(GeneratedFoodItemId.TryCreate("", out _), Is.False);
            Assert.That(GeneratedFoodItemId.TryCreate("   ", out _), Is.False);
        }

        [Test]
        public void StrictParsersRepresentEveryKnownV2EnumAndRejectUnknownValues()
        {
            var orderStates = new[]
            {
                "DRAFT", "QUEUED", "PROCESSING", "AWAITING_ADMIN_REVIEW",
                "COMPLETED", "REJECTED", "FAILED", "CANCELED"
            };
            foreach (var wireValue in orderStates)
            {
                Assert.That(YummyServiceV2ContractGuard.TryParseOrderState(wireValue, out var state), Is.True, wireValue);
                Assert.That(state, Is.Not.EqualTo(OrderState.Unknown));
            }

            var stageStates = new[]
            {
                "PENDING", "QUEUED", "PROCESSING", "COMPLETED",
                "COMPLETED_WITH_WARNING", "FAILED", "CANCELED"
            };
            foreach (var wireValue in stageStates)
            {
                Assert.That(YummyServiceV2ContractGuard.TryParseStageState(wireValue, out var state), Is.True, wireValue);
                Assert.That(state, Is.Not.EqualTo(StageState.Unknown));
            }

            var stageTypes = new[]
            {
                "INPUT_MODERATION", "EXAMPLE_RETRIEVAL", "FOOD_ANALYSIS",
                "IMAGE_TO_3D", "AUDIO_GENERATION"
            };
            foreach (var wireValue in stageTypes)
            {
                Assert.That(YummyServiceV2ContractGuard.TryParseStageType(wireValue, out var stage), Is.True, wireValue);
                Assert.That(stage, Is.Not.EqualTo(StageType.Unknown));
            }

            var artifactTypes = new[]
            {
                "SOURCE_IMAGE_ORIGINAL", "SOURCE_IMAGE_NORMALIZED", "FOOD_ANALYSIS_JSON", "GLB", "WAV"
            };
            foreach (var wireValue in artifactTypes)
            {
                Assert.That(YummyServiceV2ContractGuard.TryParseArtifactType(wireValue, out var type), Is.True, wireValue);
                Assert.That(type, Is.Not.EqualTo(ArtifactType.Unknown));
            }

            Assert.That(YummyServiceV2ContractGuard.TryParseOrderState("UNKNOWN", out var unknownState), Is.False);
            Assert.That(unknownState, Is.EqualTo(OrderState.Unknown));
            Assert.That(YummyServiceV2ContractGuard.TryParseStageState("COMPLETED_WITH_WARNINGS", out _), Is.False);
            Assert.That(YummyServiceV2ContractGuard.TryParseArtifactType("MODEL", out _), Is.False);

            Assert.That(YummyServiceV2ContractGuard.TryParseModerationStatus("PASS", out var moderation), Is.True);
            Assert.That(moderation, Is.EqualTo(ModerationStatus.Pass));
            Assert.That(YummyServiceV2ContractGuard.TryParseFoodAnalysisStatus("REVIEW_REQUIRED", out var analysisStatus), Is.True);
            Assert.That(analysisStatus, Is.EqualTo(FoodAnalysisStatus.ReviewRequired));
            Assert.That(YummyServiceV2ContractGuard.TryParseFoodAnalysisAdminDecision("APPROVED", out var adminDecision), Is.True);
            Assert.That(adminDecision, Is.EqualTo(FoodAnalysisAdminDecision.Approved));

            Assert.That(YummyServiceV2ContractGuard.TryParseStageType("NEW_STAGE", out var unknownStage), Is.False);
            Assert.That(unknownStage, Is.EqualTo(StageType.Unknown));
            Assert.That(YummyServiceV2ContractGuard.TryParseModerationStatus("ALLOW", out var unknownModeration), Is.False);
            Assert.That(unknownModeration, Is.EqualTo(ModerationStatus.Unknown));
            Assert.That(YummyServiceV2ContractGuard.TryParseFoodAnalysisStatus("VALIDATED", out var unknownAnalysis), Is.False);
            Assert.That(unknownAnalysis, Is.EqualTo(FoodAnalysisStatus.Unknown));
            Assert.That(YummyServiceV2ContractGuard.TryParseFoodAnalysisAdminDecision("ACCEPTED", out var unknownDecision), Is.False);
            Assert.That(unknownDecision, Is.EqualTo(FoodAnalysisAdminDecision.Unknown));
        }

        [Test]
        public void KnownEnumValuesRoundTripToTheExactV2WireVocabulary()
        {
            Assert.That(OrderState.AwaitingAdminReview.ToWireValue(), Is.EqualTo("AWAITING_ADMIN_REVIEW"));
            Assert.That(StageState.CompletedWithWarning.ToWireValue(), Is.EqualTo("COMPLETED_WITH_WARNING"));
            Assert.That(StageType.ImageTo3D.ToWireValue(), Is.EqualTo("IMAGE_TO_3D"));
            Assert.That(ArtifactType.FoodAnalysisJson.ToWireValue(), Is.EqualTo("FOOD_ANALYSIS_JSON"));
            Assert.That(ModerationStatus.Review.ToWireValue(), Is.EqualTo("REVIEW"));
            Assert.That(FoodAnalysisStatus.ReviewRequired.ToWireValue(), Is.EqualTo("REVIEW_REQUIRED"));
            Assert.That(FoodAnalysisAdminDecision.Approved.ToWireValue(), Is.EqualTo("APPROVED"));

            Assert.That(OrderState.Unknown.ToWireValue(), Is.Empty);
            Assert.That(StageState.Unknown.ToWireValue(), Is.Empty);
            Assert.That(StageType.Unknown.ToWireValue(), Is.Empty);
            Assert.That(ArtifactType.Unknown.ToWireValue(), Is.Empty);
        }

        [Test]
        public void ArtifactRefRequiresIdentityRevisionAnd64HexSha256()
        {
            var artifact = new ArtifactRef("artifact-1", ArtifactType.Glb, "rev-7", EmptySha256, true);

            Assert.That(artifact.IsValid, Is.True);
            Assert.That(artifact.IsVerifiedGlb, Is.True);
            Assert.That(artifact.CacheIdentity.IsValid, Is.True);
            Assert.That(artifact.CacheIdentity.ToString(), Does.Contain("artifact-1"));
            Assert.That(artifact.CacheIdentity.ToString(), Does.Contain("rev-7"));
            Assert.That(artifact.CacheIdentity.ToString(), Does.Contain(EmptySha256));

            Assert.That(new ArtifactRef("artifact-1", ArtifactType.Glb, "rev-7", "not-a-sha", true).IsValid, Is.False);
            Assert.That(new ArtifactRef("artifact-1", ArtifactType.Unknown, "rev-7", EmptySha256, true).IsValid, Is.False);
            Assert.That(new ArtifactRef("artifact-1", ArtifactType.Glb, "", EmptySha256, true).IsValid, Is.False);
            Assert.That(new ArtifactRef("artifact-1", ArtifactType.Glb, "rev-7", EmptySha256, false).IsVerifiedGlb, Is.False);
        }

        [Test]
        public void ArtifactBytesMustMatchSha256BeforeUse()
        {
            var artifact = new ArtifactRef("artifact-1", ArtifactType.Glb, "rev-7", EmptySha256, true);
            var bytes = Array.Empty<byte>();

            Assert.That(artifact.MatchesSha256(bytes), Is.True);
            Assert.That(artifact.VerifyBytes(bytes), Is.True);
            Assert.That(artifact.VerifyBytes(new byte[] { 1, 2, 3 }), Is.False);

            Assert.That(YummyServiceV2ContractGuard.TryAcceptDownloadedGlb(
                OrderState.Completed, artifact, bytes, "https://example.test/v2", "model", out var noFailure), Is.True);
            Assert.That(noFailure, Is.Null);
            Assert.That(YummyServiceV2ContractGuard.TryAcceptDownloadedGlb(
                OrderState.Completed, artifact, new byte[] { 1, 2, 3 }, "https://example.test/v2", "model", out var mismatch), Is.False);
            Assert.That(mismatch.Code, Is.EqualTo(ContractGuardFailureCode.ArtifactIntegrityMismatch));
        }

        [Test]
        public void ReadyGateRequiresCompletedOrderAndSelectedVerifiedGlb()
        {
            var glb = new ArtifactRef("artifact-glb", ArtifactType.Glb, "r1", EmptySha256, true);
            var image = new ArtifactRef("artifact-image", ArtifactType.SourceImageNormalized, "r2", EmptySha256, true);

            Assert.That(GeneratedFoodItem.MeetsMinimumReadyGate(OrderState.Completed, glb), Is.True);
            Assert.That(GeneratedFoodItem.MeetsMinimumReadyGate(OrderState.Processing, glb), Is.False);
            Assert.That(GeneratedFoodItem.MeetsMinimumReadyGate(OrderState.Completed, image), Is.False);
            Assert.That(GeneratedFoodItem.MeetsMinimumReadyGate(OrderState.Completed,
                new ArtifactRef("artifact-glb", ArtifactType.Glb, "r1", EmptySha256, false)), Is.False);
            Assert.That(GeneratedFoodItem.MeetsMinimumReadyGate(OrderState.Completed, null), Is.False);
        }

        [Test]
        public void GeneratedFoodItemRetainsFiveStagesAndAllowsRetrievalWarningOnly()
        {
            var stages = AllStages(StageState.Completed);
            stages[StageType.ExampleRetrieval] = StageState.CompletedWithWarning;
            var item = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-1"),
                OrderState.Completed,
                stages,
                new Dictionary<ArtifactType, ArtifactRef>
                {
                    { ArtifactType.Glb, new ArtifactRef("glb-1", ArtifactType.Glb, "r1", EmptySha256, true) }
                });

            Assert.That(item.HasAllStageStates, Is.True);
            Assert.That(item.IsValid, Is.True);
            Assert.That(item.IsSelectable, Is.True);
            Assert.That(item.GetStageState(StageType.ImageTo3D), Is.EqualTo(StageState.Completed));

            stages[StageType.FoodAnalysis] = StageState.CompletedWithWarning;
            var invalid = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-2"), OrderState.Completed, stages,
                new Dictionary<ArtifactType, ArtifactRef>
                {
                    { ArtifactType.Glb, new ArtifactRef("glb-2", ArtifactType.Glb, "r1", EmptySha256, true) }
                });
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.IsSelectable, Is.False);
        }

        [Test]
        public void CompatibilityGuardAcceptsReviewedRevisionAndRejectsV1OrUnknownRevision()
        {
            var accepted = YummyServiceV2Compatibility.Expected("https://example.test/v2", "catalog");
            Assert.That(YummyServiceV2ContractGuard.TryValidateCompatibility(accepted, out var noFailure), Is.True);
            Assert.That(noFailure, Is.Null);

            var v1 = new YummyServiceV2Compatibility(
                "https://example.test/v1/orders?token=secret",
                YummyServiceV2Contract.RepositoryCommit,
                YummyServiceV2Contract.OpenApiVersion,
                YummyServiceV2Contract.OpenApiSha256,
                "history");
            Assert.That(YummyServiceV2ContractGuard.TryValidateCompatibility(v1, out var v1Failure), Is.False);
            Assert.That(v1Failure.Code, Is.EqualTo(ContractGuardFailureCode.V1Rejected));
            Assert.That(v1Failure.ServerUrl, Does.Not.Contain("token"));

            var unknownRevision = new YummyServiceV2Compatibility(
                "https://example.test/v2",
                "unknown-commit",
                YummyServiceV2Contract.OpenApiVersion,
                YummyServiceV2Contract.OpenApiSha256,
                "history");
            Assert.That(YummyServiceV2ContractGuard.TryValidateCompatibility(unknownRevision, out var revisionFailure), Is.False);
            Assert.That(revisionFailure.Code, Is.EqualTo(ContractGuardFailureCode.ContractRevisionMismatch));
        }

        [Test]
        public void V1NegativeFixtureRejectsPlainV1AndDoesNotLeakCredentialsInDiagnostics()
        {
            Assert.That(YummyServiceV2ContractGuard.ContainsV1Marker("v1"), Is.True);
            Assert.That(YummyServiceV2ContractGuard.ContainsV1Marker("/v1?token=secret"), Is.True);

            var legacy = new YummyServiceV2Compatibility(
                "https://user:password@example.test/v1/orders?token=secret",
                YummyServiceV2Contract.RepositoryCommit,
                YummyServiceV2Contract.OpenApiVersion,
                YummyServiceV2Contract.OpenApiSha256,
                "/v1/orders?token=secret");

            Assert.That(YummyServiceV2ContractGuard.TryValidateCompatibility(legacy, out var failure), Is.False);
            Assert.That(failure.Code, Is.EqualTo(ContractGuardFailureCode.V1Rejected));
            Assert.That(failure.ServerUrl, Is.EqualTo("https://example.test/v1/orders"));
            Assert.That(failure.ServerUrl, Does.Not.Contain("password"));
            Assert.That(failure.ServerUrl, Does.Not.Contain("secret"));
            Assert.That(failure.Operation, Is.EqualTo("/v1/orders"));
            Assert.That(failure.Operation, Does.Not.Contain("secret"));
        }

        [Test]
        public void MissingStageOrSelectedArtifactFailsClosed()
        {
            var missingStage = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-1"), OrderState.Completed,
                new Dictionary<StageType, StageState>(),
                new Dictionary<ArtifactType, ArtifactRef>());
            Assert.That(missingStage.IsValid, Is.False);
            Assert.That(missingStage.IsSelectable, Is.False);

            var missingGlb = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-2"), OrderState.Completed,
                AllStages(StageState.Completed),
                new Dictionary<ArtifactType, ArtifactRef>());
            Assert.That(missingGlb.IsValid, Is.True);
            Assert.That(missingGlb.IsSelectable, Is.False);
        }

        [Test]
        public void ImageTo3DBranchCanRemainIndependentWhileOrderAwaitsReview()
        {
            var stages = AllStages(StageState.Pending);
            stages[StageType.InputModeration] = StageState.Completed;
            stages[StageType.FoodAnalysis] = StageState.Processing;
            stages[StageType.ImageTo3D] = StageState.Pending;
            var item = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-review"),
                OrderState.AwaitingAdminReview,
                stages,
                new Dictionary<ArtifactType, ArtifactRef>());

            Assert.That(item.IsValid, Is.True);
            Assert.That(item.IsImageTo3DBranchIndependent, Is.True);
            Assert.That(item.IsSelectable, Is.False);
        }

        [Test]
        public void GeneratedFoodItemSnapshotsStageAndArtifactInputs()
        {
            var stages = AllStages(StageState.Completed);
            var artifacts = new Dictionary<ArtifactType, ArtifactRef>
            {
                { ArtifactType.Glb, new ArtifactRef("glb-1", ArtifactType.Glb, "r1", EmptySha256, true) }
            };
            var item = new GeneratedFoodItem(
                GeneratedFoodItemId.Create("order-snapshot"),
                OrderState.Completed,
                stages,
                artifacts);

            stages[StageType.ImageTo3D] = StageState.Failed;
            artifacts.Clear();

            Assert.That(item.GetStageState(StageType.ImageTo3D), Is.EqualTo(StageState.Completed));
            Assert.That(item.TryGetSelectedGlb(out var glb), Is.True);
            Assert.That(glb.ArtifactId, Is.EqualTo("glb-1"));
        }

        private static Dictionary<StageType, StageState> AllStages(StageState state)
        {
            return new Dictionary<StageType, StageState>
            {
                { StageType.InputModeration, state },
                { StageType.ExampleRetrieval, state },
                { StageType.FoodAnalysis, state },
                { StageType.ImageTo3D, state },
                { StageType.AudioGeneration, state }
            };
        }
    }
}
