﻿using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules
{
    [TestFixture]
    [TestOf(typeof(MaxTimeRestartRuleSystem))]
    public sealed class RuleMaxTimeRestartTest
    {
        [Test]
        public async Task RestartTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
            var server = pairTracker.Pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var sGameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();

            sGameTicker.StartGameRule("MaxTimeRestart", out var ruleEntity);
            Assert.That(entityManager.TryGetComponent<MaxTimeRestartRuleComponent>(ruleEntity, out var maxTime));

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
                maxTime.RoundMaxTime = TimeSpan.FromSeconds(3);
                sGameTicker.StartRound();
            });

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            var ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundMaxTime.TotalSeconds * 1.1f);
            await PoolManager.RunTicksSync(pairTracker.Pair, ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundEndDelay.TotalSeconds * 1.1f);
            await PoolManager.RunTicksSync(pairTracker.Pair, ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
