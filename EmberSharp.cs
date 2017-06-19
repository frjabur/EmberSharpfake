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

        private Lazy<IInventoryManager> Inventory { get; }

        private TaskHandler KillStealHandler { get; set; }

        private Item Orchid { get; set; }

        private Lazy<IPrediction> Prediction { get; }

        private Item RodofAtos { get; set; }

        private Item SheepStick { get; set; }

        private Lazy<ITargetSelectorManager> TargetSelector { get; }

        private Item VeilofDiscord { get; set; }

	    private int MinimumRemnants => _minimumRemnantItem.GetValue<Slider>().Value;

        private int CurrentRemnants
            => MyHero.FindModifier("modifier_ember_spirit_fire_remnant_charge_counter")?.StackCount ?? 0;

        private IEnumerable<Unit> Remnants
         => ObjectManager.GetEntities<Unit>().Where(x => x.Name == "npc_dota_ember_spirit_fire_remnant");

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
            }

            //Vars we need before combo.
            bool HasAghanims = Owner.HasItem(ClassId.CDOTA_Item_UltimateScepter);
            float FistCost = Fist.GetManaCost(Fist.Level - 1);
            float RemnantCost = Remnant.GetManaCost(Remnant.Level - 1);
            float CurrentMana = Owner.Mana;
            float TotalMana = Owner.MaximumMana;

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

        }

        private MenuItem _minimumRemnantItem;
        private Ability _shieldAbility;
        private Ability _sleightAbility;
        private Ability _stunAbility;
        private Ability _ultAbility;
        private Ability _ultActivateAbility;
        private int MinimumRemnants => _minimumRemnantItem.GetValue<Slider>().Value;

        private int CurrentRemnants
            => MyHero.FindModifier("modifier_ember_spirit_fire_remnant_charge_counter")?.StackCount ?? 0;

        private IEnumerable<Unit> Remnants
            => ObjectManager.GetEntities<Unit>().Where(x => x.Name == "npc_dota_ember_spirit_remnant");

        public override void OnLoad()
        {
            base.OnLoad();

            var heroMenu = new Menu("Ember Spirit", "zaioEmber Spirit", false, "npc_dota_hero_ember_spirit", true);

            heroMenu.AddItem(new MenuItem("zaioEmberSpiritAbilitiesText", "Supported Abilities"));
            var supportedStuff = new MenuItem("zaioEmberSpiritAbilities", string.Empty);
            supportedStuff.SetValue(new AbilityToggler(SupportedAbilities.ToDictionary(x => x, y => true)));
            heroMenu.AddItem(supportedStuff);

            heroMenu.AddItem(new MenuItem("zaioEmberSpiritKillstealAbilitiesText", "Supported Killsteal Abilities"));
            var supportedKillsteal = new MenuItem("zaioEmberSpiritKillstealAbilities", string.Empty);
            supportedKillsteal.SetValue(new AbilityToggler(KillstealAbilities.ToDictionary(x => x, y => true)));
            heroMenu.AddItem(supportedKillsteal);

            OnLoadMenuItems(supportedStuff, supportedKillsteal);

            _minimumRemnantItem =
                new MenuItem("zaioEmberSpiritMinimumRemnant", "Save Remnants").SetValue(new Slider(1, 0, 3));
            _minimumRemnantItem.Tooltip = "Minimum Remnants to keep.";
            heroMenu.AddItem(_minimumRemnantItem);

            ZaioMenu.LoadHeroSettings(heroMenu);

            _ultActivateAbility = MyHero.GetAbilityById(AbilityId.ember_spirit_activate_fire_remnant);
            _ultAbility = MyHero.GetAbilityById(AbilityId.ember_spirit_fire_remnant);
            _shieldAbility = MyHero.GetAbilityById(AbilityId.ember_spirit_flame_guard);
            _stunAbility = MyHero.GetAbilityById(AbilityId.ember_spirit_searing_chains);
            _sleightAbility = MyHero.GetAbilityById(AbilityId.ember_spirit_sleight_of_fist);
        }

        /*
        modifier_ember_spirit_sleight_of_fist_caster
        modifier_ember_spirit_sleight_of_fist_caster_invulnerability
        */

        protected override async Task<bool> Killsteal()
        {
            if (await base.Killsteal())
            {
                return true;
            }

            if (MyHero.IsSilenced() ||
                MyHero.HasModifiers(
                    new[] {"modifier_ember_spirit_sleight_of_fist_caster", "modifier_ember_spirit_fire_remnant"}, false))
            {
                return false;
            }

            if (_sleightAbility.IsKillstealAbilityEnabled() && _sleightAbility.CanBeCasted())
            {
                var damage = MyHero.MinimumDamage + MyHero.BonusDamage +
                             _sleightAbility.GetAbilityData("bonus_hero_damage");
                var radius = _sleightAbility.GetAbilityData("radius");

                var enemy =
                    ObjectManager.GetEntitiesParallel<Hero>()
                                 .FirstOrDefault(
                                     x =>
                                         x.IsAlive && x.Team != MyHero.Team && !x.IsIllusion &&
                                         _sleightAbility.CanBeCasted(x) &&
                                         _sleightAbility.CanHit(x) &&
                                         x.Health < damage * (1 - x.PhysicalResistance()) && !x.CantBeAttacked() &&
                                         !x.CantBeKilled());
                if (enemy != null)
                {
                    Log.Debug(
                        $"use killsteal W because enough damage {enemy.Health} <= {enemy.Health < damage * (1 - enemy.PhysicalResistance())} ");

                    _sleightAbility.UseAbility(enemy.NetworkPosition);
                    await Await.Delay(125);
                    return true;
                }
            }

            if (Target != null)
            {
                return false;
            }

            if (_ultAbility.IsKillstealAbilityEnabled() && _ultAbility.CanBeCasted() && _ultActivateAbility.CanBeCasted() &&
                (MinimumRemnants == 0 || MinimumRemnants < CurrentRemnants))
            {
                var damage = _ultAbility.GetAbilityData("damage");
                damage *= GetSpellAmp();
                var enemy =
                    ObjectManager.GetEntitiesParallel<Hero>()
                                 .FirstOrDefault(
                                     x =>
                                         x.IsAlive && x.Team != MyHero.Team && !x.IsIllusion &&
                                         _ultAbility.CanBeCasted(x) &&
                                         _ultAbility.CanHit(x) && !x.IsMagicImmune() &&
                                         x.Health < damage * (1 - x.MagicResistance()) && !x.CantBeAttacked() &&
                                         !x.CantBeKilled());


                if (enemy != null)
                {
                    Log.Debug(
                        $"use killsteal ult because enough damage {enemy.Health} <= {damage * (1 - enemy.MagicResistance())} ");

                    var castPoint = _ultAbility.FindCastPoint();
                    var speed = MyHero.MovementSpeed * (_ultAbility.GetAbilityData("speed_multiplier") / 100);
                    var time = (castPoint + enemy.Distance2D(MyHero) / speed) * 1000.0f;
                    var predictedPos = Prediction.Prediction.PredictPosition(enemy, (int) time);


                    // test if we already got a remnant near the enemy
                    var radius = _ultAbility.GetAbilityData("radius");
                    var remnant = Remnants.FirstOrDefault(unit => unit.Distance2D(enemy) < radius);
                    if (remnant == null)
                    {
                        Log.Debug($"placing remnant first!");
                        _ultAbility.UseAbility(predictedPos);
                        await Await.Delay((int) (time + Game.Ping));
                    }
                    else
                    {
                        Log.Debug($"already got a remnant near the enemy PogChamp!");
                    }
                    _ultActivateAbility.UseAbility(predictedPos);
                    await Await.Delay(125);
                    return true;
                }
            }

            return false;
        }

        public override async Task ExecuteComboAsync(Unit target, CancellationToken tk = new CancellationToken())
        {
            if (
                MyHero.HasModifiers(new[]
                {
                    "modifier_ember_spirit_sleight_of_fist_caster",
                    "modifier_ember_spirit_fire_remnant"
                }, false))
            {
                Log.Debug($"in sleight mode");
                if (!MyHero.IsSilenced() && _stunAbility.IsAbilityEnabled() && _stunAbility.CanBeCasted(target) && _stunAbility.CanHit(target) &&
                    !target.IsMagicImmune())
                {
                    Log.Debug($"use our Q because we are using W or ult and are near the target!");
                    _stunAbility.UseAbility();
                    await Await.Delay(125, tk);
                }
                return;
            }
            await HasNoLinkens(target, tk);
            await UseItems(target, tk);

            // make him disabled
            if (await DisableEnemy(target, tk) == DisabledState.UsedAbilityToDisable)
            {
                Log.Debug($"disabled!");
                // return;
            }
            if (!MyHero.IsSilenced())
            {
                if (_sleightAbility.IsAbilityEnabled() && _sleightAbility.CanBeCasted(target) && _sleightAbility.CanHit(target))
                {
                    Log.Debug($"using sleigth");
                    _sleightAbility.UseAbility(target.NetworkPosition);
                    await Await.Delay(1, tk);
                    return;
                }

                if (_stunAbility.IsAbilityEnabled() && _stunAbility.CanBeCasted(target) && _stunAbility.CanHit(target))
                {
                    Log.Debug($"use our Q");
                    _stunAbility.UseAbility();
                    await Await.Delay(125, tk);
                }

                var distance = _ultAbility.CastRange;
                if (_shieldAbility.IsAbilityEnabled() && _shieldAbility.CanBeCasted())
                {
                    var hasEnemies = ObjectManager.GetEntitiesParallel<Hero>()
                                                  .Any(
                                                      x =>
                                                          x.IsValid && x.IsAlive && x.Team != MyHero.Team &&
                                                          (_ultAbility.CanBeCasted() && x.Distance2D(MyHero) < distance ||
                                                           !_ultAbility.CanBeCasted() && x.Distance2D(MyHero) < 800));
                    if (hasEnemies)
                    {
                        _shieldAbility.UseAbility();
                        await Await.Delay(125, tk);
                    }
                }

                if (!IsInRange(MyHero.AttackRange * 2.0f) && !target.IsMagicImmune())
                {
                    if (_ultAbility.IsAbilityEnabled() && _ultAbility.CanBeCasted() && _ultActivateAbility.CanBeCasted() &&
                        (MinimumRemnants == 0 || MinimumRemnants < CurrentRemnants))
                    {
                        var castPoint = _ultAbility.FindCastPoint();
                        var speed = MyHero.MovementSpeed * (_ultAbility.GetAbilityData("speed_multiplier") / 100);
                        var time = (castPoint + target.Distance2D(MyHero) / speed) * 1000.0f;
                        var predictedPos = Prediction.Prediction.PredictPosition(target, (int) time);

                        // test if we already got a remnant near the enemy
                        var radius = _ultAbility.GetAbilityData("radius");
                        var remnant = Remnants.FirstOrDefault(unit => unit.Distance2D(target) < radius);
                        if (remnant == null)
                        {
                            Log.Debug($"placing remnant first to approach!");
                            _ultAbility.UseAbility(predictedPos);
                            await Await.Delay((int) (time + Game.Ping), tk);
                        }
                        else
                        {
                            Log.Debug($"already got a remnant near the enemy PogChamp to approach!");
                        }
                        _ultActivateAbility.UseAbility(predictedPos);
                        await Await.Delay(100, tk);
                        return;
                    }
                }
            }

            // check if we are near the enemy
            if (!await MoveOrBlinkToEnemy(target, tk))
            {
                Log.Debug($"return because of blink");
                return;
            }

            if (!MyHero.IsSilenced())
            {
                if (_shieldAbility.IsAbilityEnabled() && _shieldAbility.CanBeCasted())
                {
                    _shieldAbility.UseAbility();
                    await Await.Delay(125, tk);
                }

                if (_ultAbility.IsAbilityEnabled() && _ultAbility.CanBeCasted(target) && _ultActivateAbility.CanBeCasted() && _ultAbility.CanHit(target) &&
                    !target.IsMagicImmune() &&
                    (MinimumRemnants == 0 || MinimumRemnants < CurrentRemnants))
                {
                    var damage = _ultAbility.GetAbilityData("damage");
                    damage *= GetSpellAmp();
                    if (target.Health < damage * (1 - target.MagicResistance()))
                    {
                        var castPoint = _ultAbility.FindCastPoint();
                        var speed = MyHero.MovementSpeed * (_ultAbility.GetAbilityData("speed_multiplier") / 100);
                        var time = (castPoint + target.Distance2D(MyHero) / speed) * 1000.0f;
                        var predictedPos = Prediction.Prediction.PredictPosition(target, (int) time);

                        // test if we already got a remnant near the enemy
                        var radius = _ultAbility.GetAbilityData("radius");
                        var remnant = Remnants.FirstOrDefault(unit => unit.Distance2D(target) < radius);
                        if (remnant == null)
                        {
                            Log.Debug($"placing remnant first to kill!");
                            _ultAbility.UseAbility(predictedPos);
                            await Await.Delay((int) (time + Game.Ping), tk);
                        }
                        else
                        {
                            Log.Debug($"already got a remnant near the enemy PogChamp to kill!");
                        }
                        _ultActivateAbility.UseAbility(predictedPos);
                        await Await.Delay(1, tk);
                        return;
                    }

        protected override void OnActivate()
        {
            this.KillStealHandler = UpdateManager.Run(this.KillStealAsync, true);
            this.KillStealHandler.RunAsync();

            this.Remnant = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_fire_remnant);
            this.Fist = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_sleight_of_fist);
            this.Activator = UnitExtensions.GetAbilityById(this.Owner, AbilityId.ember_spirit_activate_fire_remnant);
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