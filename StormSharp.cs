// <copyright file="StormSharp.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace StormSharpSDK
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

    public class StormSharp : KeyPressOrbwalkingModeAsync
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public StormSharp(
            Key key,
            StormSharpConfig config,
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

        public StormSharpConfig Config { get; }

        private Item BlinkDagger { get; set; }

        private Item BloodThorn { get; set; }

        private Item HurricanePike { get; set; }

        private Item ShivasGuard { get; set; }

        private Item Mjollnir { get; set; }

        private Ability Vortex { get; set; }

        private Ability Overload { get; set; }

        private Ability Lightning { get; set; }

        private Ability Remnant { get; set; }

        private Lazy<IInventoryManager> Inventory { get; }

        private TaskHandler KillStealHandler { get; set; }

        private Item Orchid { get; set; }

        private Lazy<IPrediction> Prediction { get; }

        private Item RodofAtos { get; set; }

        private Item SheepStick { get; set; }

        private Lazy<ITargetSelectorManager> TargetSelector { get; }

        private Item VeilofDiscord { get; set; }

        private IEnumerable<Unit> Remnants
         => ObjectManager.GetEntities<Unit>().Where(x => x.Name == "npc_dota_storm_spirit_static_remnant");

        public override async Task ExecuteAsync(CancellationToken token)
        {
            this.KillStealHandler.RunAsync();

            var target = this.TargetSelector.Value.Active.GetTargets().FirstOrDefault(x => !x.IsInvulnerable() && !UnitExtensions.IsMagicImmune(x) && x.IsAlive);

            var silenced = UnitExtensions.IsSilenced(this.Owner);

            var sliderValue = this.Config.UseBlinkPrediction.Item.GetValue<Slider>().Value;

            if (this.BlinkDagger != null &&
            this.BlinkDagger.IsValid &&
            target != null && Owner.Distance2D(target) <= 1200 + sliderValue && !(Owner.Distance2D(target) <= 400) &&
            this.BlinkDagger.CanBeCasted(target) &&
            this.Config.ItemToggler.Value.IsEnabled(this.BlinkDagger.Name))
            {
                var l = (this.Owner.Distance2D(target) - sliderValue) / sliderValue;
                var posA = this.Owner.Position;
                var posB = target.Position;
                var x = (posA.X + (l * posB.X)) / (1 + l);
                var y = (posA.Y + (l * posB.Y)) / (1 + l);
                var position = new Vector3((int)x, (int)y, posA.Z);

                Log.Debug("Using BlinkDagger");
                this.BlinkDagger.UseAbility(position);
                await Await.Delay(this.GetItemDelay(target), token);
            }
            //Are we in an ult phase?
            var inUltimate = UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_ball_lightning") || Lightning.IsInAbilityPhase;

            //Check if we're silenced, our target is alive, and we have a target.
            var UltDistance = Config.DistanceForUlt.Item.GetValue<Slider>().Value;

            //Check for distance to target and push against slider value
            if (target != null && target.IsAlive
                && Owner.Distance2D(target) >= 400 && Owner.Distance2D(target) <= UltDistance
                && Config.AbilityToggler.Value.IsEnabled(Lightning.Name) && !silenced)
            {
                //Based on whether they are moving or not, predict where they will be.
                if (target.IsMoving)
                {
                    var PredictedPosition = EnsagePredict.InFront(target, 200);
                    //Check the mana consumed from our prediction.
                    double TempManaConsumed = (Lightning.GetAbilityData("ball_lightning_initial_mana_base") + ((Lightning.GetAbilityData("ball_lightning_initial_mana_percentage") / 100) * Owner.MaximumMana))
                            + ((Ensage.SDK.Extensions.EntityExtensions.Distance2D(Owner, PredictedPosition) / 100) * (((Lightning.GetAbilityData("ball_lightning_travel_cost_percent") / 100) * Owner.MaximumMana)));
                    if (TempManaConsumed <= Owner.Mana && !inUltimate)
                    {
                        Lightning.UseAbility(PredictedPosition);
                        await Await.Delay((int)(Lightning.FindCastPoint() + Owner.GetTurnTime(PredictedPosition) * 2250 + Game.Ping), token);
                    }
                }

                else
                {
                    var PredictedPosition = target.NetworkPosition;
                    double TempManaConsumed = (Lightning.GetAbilityData("ball_lightning_initial_mana_base") + ((Lightning.GetAbilityData("ball_lightning_initial_mana_percentage") / 100) * Owner.MaximumMana))
                           + ((Ensage.SDK.Extensions.EntityExtensions.Distance2D(Owner, PredictedPosition) / 100) * (((Lightning.GetAbilityData("ball_lightning_travel_cost_percent") / 100) * Owner.MaximumMana)));
                    if (TempManaConsumed <= Owner.Mana && !inUltimate)
                    {
                        Lightning.UseAbility(PredictedPosition);
                        await Await.Delay((int)(Lightning.FindCastPoint() + Owner.GetTurnTime(PredictedPosition) * 2250 + Game.Ping), token);
                    }

                }

            }

            //Vars we need before combo.
            bool HasAghanims = Owner.HasItem(ClassId.CDOTA_Item_UltimateScepter);
            float VortexCost = Vortex.GetManaCost(Vortex.Level - 1);
            float RemnantCost = Remnant.GetManaCost(Remnant.Level - 1);
            float CurrentMana = Owner.Mana;
            float TotalMana = Owner.MaximumMana;

            //This is here to stop us from ulting after our target dies.
            float RemnantAutoDamage = this.Remnant.GetAbilityData("static_remnant_damage") + this.Overload.GetDamage(Overload.Level - 1);
            RemnantAutoDamage += (Owner.MinimumDamage + Owner.BonusDamage);
            RemnantAutoDamage *= GetSpellAmp();


            var RemnantAutokillableTar =
            ObjectManager.GetEntitiesParallel<Hero>()
                .FirstOrDefault(
                    x =>
                        x.IsAlive && x.Team != this.Owner.Team && !x.IsIllusion
                        && this.Remnant.CanBeCasted() && this.Remnant.CanHit(x)
                        && x.Health < (RemnantAutoDamage * (1 - x.MagicDamageResist))
                        && !UnitExtensions.IsMagicImmune(x)
                        && x.Distance2D(this.Owner) <= 235);

            var ActiveRemnant = Remnants.Any(unit => unit.Distance2D(RemnantAutokillableTar) < 240);


            if (!silenced && target != null)
            {
                //there is a reason behind this; the default delay on storm ult is larger than a minimum distance travelled.
                var TargetPosition = target.NetworkPosition;
                /* TargetPosition *= 100;
                TargetPosition = target.NetworkPosition + TargetPosition;*/
                double ManaConsumed = (Lightning.GetAbilityData("ball_lightning_initial_mana_base") + ((Lightning.GetAbilityData("ball_lightning_initial_mana_percentage") / 100) * CurrentMana))
                    + ((Ensage.SDK.Extensions.EntityExtensions.Distance2D(Owner, TargetPosition) / 100) * (((Lightning.GetAbilityData("ball_lightning_travel_cost_percent") / 100) * CurrentMana)));

                //Always auto attack if we have an overload charge.
                if (UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_overload") && target != null)
                {
                    Owner.Attack(target);
                    await Await.Delay(500);
                }

                //Vortex prioritization logic [do we have q/w enabled, do we have the mana to cast both, do they have lotus, do we have an overload modifier]
                if (!UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_overload") &&
                    Config.AbilityToggler.Value.IsEnabled(Vortex.Name) && Vortex.CanBeCasted()
                    && Config.AbilityToggler.Value.IsEnabled(Remnant.Name) && Remnant.CanBeCasted()
                    && (VortexCost + RemnantCost) <= CurrentMana)
                {
                    //Use Vortex
                    if (!HasAghanims)
                    {
                        Vortex.UseAbility(target);
                        await Await.Delay(GetAbilityDelay(Owner, Vortex), token);
                    }

                    //Use Vortex differently for aghanims.
                    else
                    {
                        Vortex.UseAbility();
                        await Await.Delay(GetAbilityDelay(Owner, Vortex), token);
                    }
                }

                //Remnant logic [w is not available, cant ult, close enough for the detonation]
                if (!UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_overload") && target.IsAlive &&
                    Config.AbilityToggler.Value.IsEnabled(Remnant.Name) && Remnant.CanBeCasted()
                    && !Vortex.CanBeCasted() && (CurrentMana <= RemnantCost + ManaConsumed || Owner.Distance2D(target) <= Remnant.GetAbilityData("static_remnant_radius")))
                {
                    Remnant.UseAbility();
                    await Await.Delay(GetAbilityDelay(Owner, Remnant), token);
                }

                //Ult logic [nothing else is available or we are not in range for a q]
                if (!UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_overload") && target.IsAlive &&
                    Config.AbilityToggler.Value.IsEnabled(Lightning.Name) && Lightning.CanBeCasted()
                    && (!Remnant.CanBeCasted() || Owner.Distance2D(target) >= Remnant.GetAbilityData("static_remnant_radius"))
                    && (!Vortex.CanBeCasted(target) || Owner.Distance2D(target) <= UltDistance)
                    //Don't cast ult if theres a remnant that can kill our target.
                    && !inUltimate && (RemnantAutokillableTar == null || ActiveRemnant == false))
                //todo: alternate check for aghanims
                {
                    Lightning.UseAbility(target.Position, 300); //TargetPosition 
                    int delay = (int)((Lightning.FindCastPoint() + Owner.GetTurnTime(TargetPosition)) * 1250.0 + Game.Ping);
                    Log.Debug($"{delay}ms to wait.");
                    await Task.Delay(delay);
                }
            }



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
            float RemnantAutoDamage = this.Remnant.GetAbilityData("static_remnant_damage") + this.Overload.GetDamage(Overload.Level - 1);
            RemnantAutoDamage += (Owner.MinimumDamage + Owner.BonusDamage);
            RemnantAutoDamage *= GetSpellAmp();

            float AutoDamage = this.Overload.GetDamage(Overload.Level - 1);
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

            if (UnitExtensions.HasModifier(Owner, "modifier_storm_spirit_overload") && AutokillableTar != null)
            {
                Owner.Attack(AutokillableTar);
                await Await.Delay(500);
            }
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

            this.Remnant = UnitExtensions.GetAbilityById(this.Owner, AbilityId.storm_spirit_static_remnant);
            this.Vortex = UnitExtensions.GetAbilityById(this.Owner, AbilityId.storm_spirit_electric_vortex);
            this.Lightning = UnitExtensions.GetAbilityById(this.Owner, AbilityId.storm_spirit_ball_lightning);
            this.Overload = UnitExtensions.GetAbilityById(this.Owner, AbilityId.storm_spirit_overload);

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
