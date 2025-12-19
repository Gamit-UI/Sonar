using System.IO.Compression;
using ConcurrentCollections;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sonar.Events.Extensions;
using Sonar.Rules;
using Sonar.Rules.Aggregations;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Serialization;
using Sonar.Rules.Serialization.Yaml.Converters;
using Sonar.Rules.Serialization.Yaml.Deserializers;
using Sonar.Rules.Serialization.Yaml.Extensions;
using Sonar.Rules.Serialization.Yaml.Resolvers;
using Sonar.Tests.Detection.EventRecords;
using Sonar.Tests.Detection.Rules;
using Sonar.Tests.Detection.Rules._5b0b75dc_9190_4047_b9a8_14164cee8a31;
using Sonar.Tests.Detection.Rules.e8f382bc_a0ae_9af8_e389_db89f741f5e0;
using Xunit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeTypeResolvers;
using YAXLib;
using YAXLib.Enums;
using YAXLib.Exceptions;
using YAXLib.Options;
using TestData_Matched = Sonar.Tests.Detection.Rules.e8f382bc_a0ae_9af8_e389_db89f741f5e0.TestData_Matched;
using TestData_Unmatched_1 = Sonar.Tests.Detection.Rules.a4504cb2_23f6_6d94_5ae6_d6013cf1d995.TestData_Unmatched_1;
using TestData_Unmatched_2 = Sonar.Tests.Detection.Rules.a4504cb2_23f6_6d94_5ae6_d6013cf1d995.TestData_Unmatched_2;

namespace Sonar.Tests.Detection;

[Collection(nameof(RuleCollection))]
public class DetectionTests(ITestOutputHelper testOutputHelper, RuleFixture fixture)
{
    private readonly IDeserializer deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithTypeConverter(new DynamicObjectConverter())
        .WithNodeTypeResolver(new MappingNodeResolver(), ls => ls.InsteadOf<DefaultContainersNodeTypeResolver>())
        .WithNodeDeserializer(new ListsAcceptScalarDeserializer())
        .WithNodeDeserializer(new ForceEmptyListsOnDeserialization())
        .Build();

    [Fact]
    public async Task Hayabusa_TryMatch_Should_BeTrue()
    {
        var serializer = new YAXSerializer<YaxEventRecord>(new SerializerOptions { SerializationOptions = YAXSerializationOptions.DontSerializeNullObjects });
        using var client = new HttpClient();
        await using var response = await client.GetStreamAsync("https://github.com/Yamato-Security/hayabusa-rules/archive/refs/heads/main.zip", CancellationToken.None);
        await using var archive = new ZipArchive(response);
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.Contains("hayabusa/builtin", StringComparison.OrdinalIgnoreCase) && !entry.FullName.Contains("sigma/builtin", StringComparison.OrdinalIgnoreCase)) continue;
            await using var stream = await entry.OpenAsync();
            using var streamReader = new StreamReader(stream);
            foreach (var value in deserializer.DeserializeMany<IDictionary<string, object>>(streamReader))
            {
                if ((!value.TryGetValue("hayabusa-sample-evtx", out var sample) && !value.TryGetValue("sample-evtx", out sample)) || sample is not string evtx || string.IsNullOrWhiteSpace(evtx)) continue;
                try
                {
                    var deserialized = serializer.Deserialize(evtx);
                    if (deserialized?.System is null) continue;
                    if (deserialized.EventData?.Items is null) continue;
                    if (deserialized.EventData.Items.Any(item => string.IsNullOrWhiteSpace(item.Name))) continue;
                    if (deserialized.EventData.Items.Any(item => string.IsNullOrWhiteSpace(item.Value))) continue;
                    if (value.TryGetValue("id", out var ruleId) && ruleId is string id && fixture.StandardRules.TryGetValue(id, out var rule))
                    {
                        var system = new Dictionary<string, string>
                        {
                            { WinEventExtensions.ProviderNameKey, deserialized.System.Provider?.Name ?? throw new ArgumentNullException(WinEventExtensions.ProviderNameKey) },
                            { WinEventExtensions.EventIdKey, deserialized.System.EventID.ToString() },
                            { WinEventExtensions.ChannelKey, deserialized.System.Channel ?? throw new ArgumentNullException(WinEventExtensions.ChannelKey) },
                            { WinEventExtensions.ComputerKey, deserialized.System.Computer ?? throw new ArgumentNullException(WinEventExtensions.ComputerKey) }
                        };

                        var eventData = new Dictionary<string, string>();
                        foreach (var node in deserialized.EventData.Items)
                        {
                            if (!string.IsNullOrWhiteSpace(node.Name) && !string.IsNullOrWhiteSpace(node.Value))
                            {
                                eventData.Add(node.Name, node.Value);
                            }
                        }

                        var winEvent = new WinEvent(system, eventData);
                        rule.TryMatch(NullLogger.Instance, winEvent, out var match).Should().BeTrue();
                        testOutputHelper.WriteLine($"[{rule.Metadata.Title}] {match.DetectionDetails.Details}");
                    }
                }
                catch (YAXBadlyFormedXML)
                {
                    continue;
                }
            }
        }
    }

    [Theory]
    [ClassData(typeof(RuleTestData<TestData_Matched>))]
    [ClassData(typeof(RuleTestData<TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.a4504cb2_23f6_6d94_5ae6_d6013cf1d995.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<TestData_Unmatched_1>))]
    [ClassData(typeof(RuleTestData<TestData_Unmatched_2>))]
    [ClassData(typeof(RuleTestData<Rules.cbc8ce50_f4cb_3b1a_647d_d943db6f0536.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.cbc8ce50_f4cb_3b1a_647d_d943db6f0536.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<TestData_Matched_1>))]
    [ClassData(typeof(RuleTestData<TestData_Matched_2>))]
    [ClassData(typeof(RuleTestData<Rules._5b0b75dc_9190_4047_b9a8_14164cee8a31.TestData_Unmatched_1>))]
    [ClassData(typeof(RuleTestData<Rules._5b0b75dc_9190_4047_b9a8_14164cee8a31.TestData_Unmatched_2>))]
    [ClassData(typeof(RuleTestData<TestData_Unmatched_3>))]
    [ClassData(typeof(RuleTestData<TestData_Unmatched_4>))]
    [ClassData(typeof(RuleTestData<Rules._176cddad_09e5_95d1_e061_52b79cdbd6b7.TestData_Matched_1>))]
    [ClassData(typeof(RuleTestData<Rules._176cddad_09e5_95d1_e061_52b79cdbd6b7.TestData_Matched_2>))]
    [ClassData(typeof(RuleTestData<Rules._176cddad_09e5_95d1_e061_52b79cdbd6b7.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._7ac85830_5907_5206_2d25_490b3ace5587.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._7ac85830_5907_5206_2d25_490b3ace5587.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.eee79b85_c08d_d643_8662_1b0498ac07b0.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.eee79b85_c08d_d643_8662_1b0498ac07b0.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.f2b2d6f5_92ed_d0f5_25fe_38019bd55906.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.f2b2d6f5_92ed_d0f5_25fe_38019bd55906.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._24e2ce91_6438_41b5_d23e_48e775ae72bd.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._24e2ce91_6438_41b5_d23e_48e775ae72bd.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.afc0e7da_4e96_1953_3fa3_8e9112c06c1c.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.afc0e7da_4e96_1953_3fa3_8e9112c06c1c.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._411ab182_6791_b9e2_733a_9f87c0448035.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._411ab182_6791_b9e2_733a_9f87c0448035.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._37e4024a_6c80_4d8f_b95d_2e7e94f3a8d1.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._37e4024a_6c80_4d8f_b95d_2e7e94f3a8d1.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._95a0be22_3e44_b097_f78c_c64a5a6dd761.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._95a0be22_3e44_b097_f78c_c64a5a6dd761.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.f224a2b6_2db1_a1a2_42d4_25df0c460915.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.f224a2b6_2db1_a1a2_42d4_25df0c460915.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules.SpecialKeywords.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules.SpecialKeywords.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._56a1bb6f_e039_3f65_3ea0_de425cefa8a7.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._56a1bb6f_e039_3f65_3ea0_de425cefa8a7.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._962b9ac0_e674_1e9c_b0d9_8a11e5dff4b4.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._5c67a566_7829_eb05_4a1f_0eb292ef993f.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._5c67a566_7829_eb05_4a1f_0eb292ef993f.TestData_Unmatched>))]
    [ClassData(typeof(RuleTestData<Rules._6683ccd7_da7a_b988_1683_7f7a1bf72bf6.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._2f97f9ce_7a7d_959a_856a_f32ca7058c3e.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._308a3356_4624_7c95_24df_cf5a02e5eb56.TestData_Matched>))]
    [ClassData(typeof(RuleTestData<Rules._59a6acf8_6a2e_3b12_d962_e991de4770a6.TestData_Unmatched>))]
    public async Task TryMatch_Should_BeTrue(string yamlRule, IList<WinEvent> winEvents, bool match, string? details)
    {
        Helper.TryGetRule(yamlRule, out var rule, out var properties, out var error).Should().BeTrue();
        error.Should().BeNullOrEmpty();
        properties.Should().NotBeNull();
        if (rule is StandardRule standardRule)
        {
            foreach (var winEvent in winEvents)
            {
                standardRule.TryMatch(NullLogger.Instance, winEvent, out var ruleMatch).Should().Be(match);
                if (match)
                {
                    ruleMatch.WinEvent.Should().BeEquivalentTo(winEvent);
                    ruleMatch.Date.Should().Be(winEvent.SystemTime);
                    ruleMatch.DetectionDetails.RuleMetadata.Id.Should().Be(rule.Id);
                    ruleMatch.DetectionDetails.RuleMetadata.Should().BeEquivalentTo(rule.Metadata);
                    ruleMatch.DetectionDetails.Details.Should().StartWith(details);
                }
            }
        }
        else if (rule is AggregationRule aggregationRule)
        {
            var propertyStore = new Mock<IPropertyStore>();
            propertyStore.Setup(x => x.GetProperties(It.Is<string>(ruleId => ruleId == rule.Id))).Returns(new ConcurrentHashSet<string>(properties ?? new HashSet<string>()));
            var repository = new AggregationRepository(NullLogger<AggregationRepository>.Instance, new Mock<IHostApplicationLifetime>().Object);
            Aggregator.Instance = new Aggregator(repository, propertyStore.Object);
            var winEventsByRule = new Dictionary<AggregationRule, IEnumerable<WinEvent>> { { aggregationRule, winEvents.Where(winEvent => aggregationRule.TryMatch(NullLogger.Instance, winEvent)) } };
            await Aggregator.Instance.AddAsync(winEventsByRule, CancellationToken.None);
            aggregationRule.TryMatch(NullLogger.Instance, out var ruleMatch).Should().Be(match);
            if (match)
            {
                ruleMatch.Match.Should().Be(match);
                ruleMatch.Date.Should().Be(winEvents.First().SystemTime);
                ruleMatch.DetectionDetails.RuleMetadata.Id.Should().Be(rule.Id);
                ruleMatch.DetectionDetails.RuleMetadata.Should().BeEquivalentTo(rule.Metadata);
                ruleMatch.DetectionDetails.Details.Should().StartWith(details);

                var query = Aggregator.Instance.Query(rule.Id, "SELECT * FROM Aggregations").ToList();
                query.Should().NotBeEmpty();
                query.Should().HaveCount(winEvents.Count);
                await Aggregator.Instance.TrimExpiredAsync(winEventsByRule, CancellationToken.None);
                Aggregator.Instance.Query(rule.Id, "SELECT * FROM Aggregations").Should().BeEmpty();
            }
        }
    }
}