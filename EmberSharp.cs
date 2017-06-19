// <copyright file="EmberSharp.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace EmberSharpSDK
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Ensage;
    using Ensage.Common.Enums;
    using Ensage.Common.Extensions;
    using Ensage.Common.Menu;
    using Ensage.Common.Threading;
    using Ensage.SDK.Extensions;
    using Ensage.SDK.Handlers;
    using Ensage.SDK.Helpers;
    using Ensage.SDK.Input;
    using Ensage.SDK.Inventory;
    using Ensage.SDK.Orbwalker;
    using Ensage.SDK.Orbwalker.Modes;
    using Ensage.SDK.Prediction;
    using EnsagePredict = Ensage.Common.Prediction;
    using Ensage.SDK.TargetSelector;

    using log4net;

    using PlaySharp.Toolkit.Logging;

    using SharpDX;

    using AbilityId = Ensage.AbilityId;
    using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;
    using System.Collections.Generic;

    public class EmberSharp : KeyPressOrbwalkingModeAsync
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public EmberSharp(
            Key key,
            EmberSharpConfig config,
            Lazy<IOrbwalkerManager> orbwalker,
            Lazy<IInputManager> input,
            Lazy<IInventoryManager> inventory,
            Lazy<ITargetSelectorManager> targetselector,
            Lazy<IPrediction> prediction)
            : base(orbwalker.Value, input.Value, key)
        {
            this.Config = config;
            this.TargetSelector = targetselector;
            this.Inventory = inventory;
            this.Prediction = prediction;
        }

        public EmberSharpConfig Config { get; }

        private Item BlinkDagger { get; set; }

        private Item BloodThorn { get; set; }

        private Item HurricanePike { get; set; }

        private Item ShivasGuard { get; set; }

        private Item Mjollnir { get; set; }

        private Ability Fist { get; set; }

        private Ability Chains { get; set; }

        private Ability Activator { get; set; }

        private Ability Remnant { get; set; }

		private Ability Flame { get; set; }

        private Lazy<IInventoryManager> Inventory { get; }

        private TaskHandler KillStealHandler { get; set; }

        private Item Orchid { get; set; }

        private Lazy<IPrediction> Prediction { get; }

        private Item RodofAtos { get; set; }

        private Item SheepStick { get; set; }

        private Lazy<ITargetSelectorManager> TargetSelector { get; }

        private Item VeilofDiscord { get; set; }

        private IEnumerable<Unit> Remnants
         => ObjectManager.GetEntities<Unit>().Where(x => x.Name == "npc_dota_ember_spirit_fire_remnant");



            if (this.BloodThorn != null &&
                this.BloodThorn.IsValid &&
                target != null &&
                this.BloodThorn.CanBeCasted(target) &&
                this.Config.ItemToggler.Value.IsEnabled(this.BloodThorn.Name))
            {
                Log.Debug("Using Bloodthorn");
                this.BloodThorn.UseAbility(target);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.SheepStick != null &&
                this.SheepStick.IsValid &&
                target != null &&
                this.SheepStick.CanBeCasted(target) &&
                this.Config.ItemToggler.Value.IsEnabled("item_sheepstick"))
            {
                Log.Debug("Using Sheepstick");
                this.SheepStick.UseAbility(target);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.Orchid != null && this.Orchid.IsValid && target != null && this.Orchid.CanBeCasted(target) && this.Config.ItemToggler.Value.IsEnabled("item_orchid"))
            {
                Log.Debug("Using Orchid");
                this.Orchid.UseAbility(target);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.RodofAtos != null &&
                this.RodofAtos.IsValid &&
                target != null &&
                this.RodofAtos.CanBeCasted(target) &&
                this.Config.ItemToggler.Value.IsEnabled("item_rod_of_atos"))
            {
                Log.Debug("Using RodofAtos");
                this.RodofAtos.UseAbility(target);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.VeilofDiscord != null &&
                this.VeilofDiscord.IsValid &&
                target != null &&
                this.VeilofDiscord.CanBeCasted() &&
                this.Config.ItemToggler.Value.IsEnabled("item_veil_of_discord"))
            {
                Log.Debug("Using VeilofDiscord");
                this.VeilofDiscord.UseAbility(target.Position);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.HurricanePike != null &&
                this.HurricanePike.IsValid &&
                target != null &&
                this.HurricanePike.CanBeCasted() &&
                this.Config.ItemToggler.Value.IsEnabled("item_hurricane_pike"))
            {
                Log.Debug("Using HurricanePike");
                this.HurricanePike.UseAbility(target);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.ShivasGuard != null &&
            this.ShivasGuard.IsValid &&
            target != null &&
            this.ShivasGuard.CanBeCasted() &&
            Owner.Distance2D(target) <= 900 &&
            this.Config.ItemToggler.Value.IsEnabled("item_shivas_guard"))
            {
                Log.Debug("Using Shiva's Guard");
                this.ShivasGuard.UseAbility();
                await Await.Delay((int)Game.Ping + 20, token);
            }

            if (this.Mjollnir != null &&
            this.Mjollnir.IsValid &&
            target != null &&
            this.Mjollnir.CanBeCasted() &&
            this.Config.ItemToggler.Value.IsEnabled("item_mjollnir"))
            {
                Log.Debug("Using Mjollnir");
                this.Mjollnir.UseAbility(Owner);
                await Await.Delay(this.GetItemDelay(target), token);
            }

            if (this.Orbwalker.OrbwalkTo(target))
            {
                return;
            }

            await Await.Delay(125, token);

        }

        protected float GetSpellAmp()
        {
            // spell amp
            var me = Context.Owner as Hero;
            var spellAmp = (100.0f + me.TotalIntelligence / 16.0f) / 100.0f;

            var aether = Owner.GetItemById(ItemId.item_aether_lens);
            if (aether != null)
            {
                spellAmp += aether.AbilitySpecialData.First(x => x.Name == "spell_amp").Value / 100.0f;
            }

            var talent =
                Owner.Spellbook.Spells.FirstOrDefault(
                    x => x.Level > 0 && x.Name.StartsWith("special_bonus_spell_amplify_"));

            if (talent != null)
            {
                spellAmp += talent.AbilitySpecialData.First(x => x.Name == "value").Value / 100.0f;
            }

            return spellAmp;
        }

        public virtual async Task KillStealAsync(CancellationToken args)
        {
            float RemnantAutoDamage = this.Remnant.GetAbilityData("fire_remnant_damage");
            RemnantAutoDamage += (Owner.MinimumDamage + Owner.BonusDamage);
            RemnantAutoDamage *= GetSpellAmp();

            float AutoDamage = this.Chains.GetDamage(Chains.Level - 1);
            AutoDamage += (Owner.MinimumDamage + Owner.BonusDamage);
            AutoDamage *= GetSpellAmp();

            var RemnantAutokillableTar =
            ObjectManager.GetEntitiesParallel<Hero>()
                 .FirstOrDefault(
                     x =>
                         x.IsAlive && x.Team != this.Owner.Team && !x.IsIllusion
                         && this.Remnant.CanBeCasted() && this.Remnant.CanHit(x)
                         && x.Health < (RemnantAutoDamage * (1 - x.MagicDamageResist))
                         && !UnitExtensions.IsMagicImmune(x)
                         && x.Distance2D(this.Owner) <= 235);

            var AutokillableTar =
            ObjectManager.GetEntitiesParallel<Hero>()
                         .FirstOrDefault(
                             x =>
                                 x.IsAlive && x.Team != this.Owner.Team && !x.IsIllusion
                                 && x.Health < AutoDamage * (1 - x.MagicDamageResist)
                                 && !UnitExtensions.IsMagicImmune(x)
                                 && x.Distance2D(this.Owner) <= 480);

            if (UnitExtensions.HasModifier(Owner, "modifier_ember_spirit_fire_remnant") && AutokillableTar != null)
            {
                Owner.Attack(AutokillableTar);
                await Await.Delay(500);
            }
        }

		protected int GetRemnantDamage(Unit unit)
        {
            return (int)Math.Floor((this.Remnant.GetAbilitySpecialData("damage") * (1 - unit.MagicDamageResist)) - (unit.HealthRegeneration * 5)); // testeeeeeeeeeeeeeeeeeeeeeeee
        }

        protected int GetAbilityDelay(Unit unit, Ability ability)
        {
            return (int)(((ability.FindCastPoint() + this.Owner.GetTurnTime(unit)) * 1000.0) + Game.Ping) + 50;
        }

        protected int GetItemDelay(Unit unit)
        {
            return (int)((this.Owner.GetTurnTime(unit) * 1000.0) + Game.Ping) + 100;
        }

        protected int GetItemDelay(Vector3 pos)
        {
            return (int)((this.Owner.GetTurnTime(pos) * 1000.0) + Game.Ping) + 100;
        }

        protected override void OnActivate()
        {
            this.KillStealHandler = UpdateManager.Run(this.KillStealAsync, true);
            this.KillStealHandler.RunAsync();

            this.Remnant = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_fire_remnant);
            this.Fist = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_sleight_of_fist);
            this.Activator = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_activate_fire_remnant);
			this.Flame = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_flame_guard);
            this.Chains = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_searing_chains);

            foreach (var item in Inventory.Value.Items)
            {
                switch (item.Id)
                {
                    case Ensage.AbilityId.item_bloodthorn:
                        this.BloodThorn = item.Item;
                        break;

                    case Ensage.AbilityId.item_sheepstick:
                        this.SheepStick = item.Item;
                        break;

                    case Ensage.AbilityId.item_hurricane_pike:
                        this.HurricanePike = item.Item;
                        break;

                    case Ensage.AbilityId.item_blink:
                        this.BlinkDagger = item.Item;
                        break;

                    case Ensage.AbilityId.item_orchid:
                        this.Orchid = item.Item;
                        break;
                    case Ensage.AbilityId.item_rod_of_atos:
                        this.RodofAtos = item.Item;
                        break;

                    case Ensage.AbilityId.item_veil_of_discord:
                        this.VeilofDiscord = item.Item;
                        break;

                    case Ensage.AbilityId.item_shivas_guard:
                        this.ShivasGuard = item.Item;
                        break;

                    case Ensage.AbilityId.item_mjollnir:
                        this.Mjollnir = item.Item;
                        break;
                }
            }

            this.Inventory.Value.CollectionChanged += this.OnInventoryChanged;

            base.OnActivate();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            this.Inventory.Value.CollectionChanged -= this.OnInventoryChanged;
        }

        private void OnInventoryChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in args.NewItems.OfType<InventoryItem>())
                {
                    switch (item.Id)
                    {
                        case Ensage.AbilityId.item_bloodthorn:
                            this.BloodThorn = item.Item;
                            break;

                        case Ensage.AbilityId.item_sheepstick:
                            this.SheepStick = item.Item;
                            break;

                        case Ensage.AbilityId.item_hurricane_pike:
                            this.HurricanePike = item.Item;
                            break;

                        case Ensage.AbilityId.item_blink:
                            this.BlinkDagger = item.Item;
                            break;

                        case Ensage.AbilityId.item_orchid:
                            this.Orchid = item.Item;
                            break;
                        case Ensage.AbilityId.item_rod_of_atos:
                            this.RodofAtos = item.Item;
                            break;

                        case Ensage.AbilityId.item_veil_of_discord:
                            this.VeilofDiscord = item.Item;
                            break;

                        case Ensage.AbilityId.item_shivas_guard:
                            this.ShivasGuard = item.Item;
                            break;

                        case Ensage.AbilityId.item_mjollnir:
                            this.Mjollnir = item.Item;
                            break;
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in args.OldItems.OfType<InventoryItem>())
                {
                    switch (item.Id)
                    {
                        case Ensage.AbilityId.item_bloodthorn:
                            this.BloodThorn = null;
                            break;

                        case Ensage.AbilityId.item_sheepstick:
                            this.SheepStick = null;
                            break;

                        case Ensage.AbilityId.item_hurricane_pike:
                            this.HurricanePike = null;
                            break;

                        case Ensage.AbilityId.item_blink:
                            this.BlinkDagger = null;
                            break;

                        case Ensage.AbilityId.item_orchid:
                            this.Orchid = null;
                            break;
                        case Ensage.AbilityId.item_rod_of_atos:
                            this.RodofAtos = null;
                            break;

                        case Ensage.AbilityId.item_veil_of_discord:
                            this.VeilofDiscord = null;
                            break;

                        case Ensage.AbilityId.item_shivas_guard:
                            this.ShivasGuard = item.Item;
                            break;

                        case Ensage.AbilityId.item_mjollnir:
                            this.Mjollnir = item.Item;
                            break;
                    }
                }
            }
        }
    }
}