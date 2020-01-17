using Assets.Scripts;
using Assets.Scripts.Common.Helpers;
using Assets.Scripts.Ships.Data;
using Assets.Scripts.Ships.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TradeRouteSystem : ComponentSystem
{
    public TradeRoutesSettingsData Settings;

    public void Init(TradeRoutesSettingsData settings)
    {
        Settings = settings;

        foreach (var route in settings.TradeRoutesList)
        {
            route.CreateEntity();
        }
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref UniqueId id,
                            ref TradeRoute comp,
                            ref DataContent data) =>
        {
            if (comp.InTransit.IsTrue() && !comp.Timer.IsExisting())
            {
                var d = data.Get<TradeRouteData>();
                d.ResourcesPrice.GiveNextUpdate(PostUpdateCommands);

                if (comp.Production.IsTrue())
                {
                    var value = (SessionFloatValue)d.MinResources.Value;
                    var amount = UnityEngine.Random.Range(d.MinResources.Amount, d.MaxResources.Amount);
                    value.ChangeNextUpdate(amount, PostUpdateCommands);
                    d.AdditionalResources(PostUpdateCommands);
                }

                if (comp.FleetId.IsExisting())
                {
                    var f = comp.FleetId.Find<Fleet>();
                    f.InRoute = 0;
                    comp.FleetId.Replace(f);
                }

                comp.IsAborted = 0;
                comp.InTransit = 0;
                comp.FleetId = UniqueId.Nil;
                PostUpdateCommands.SetComponent(id.GetEntity(), comp);
            }
        });
    }

    public UniqueId[] GetList()
    {
        var result = new List<UniqueId>();
        Entities.ForEach((ref UniqueId id,
                            ref TradeRoute comp) =>
        {
            result.Add(id);
        });
        return result.ToArray();
    }

    public void SetActive(UniqueId routeId, bool isActive)
    {
        var r = routeId.Find<TradeRoute>();
        r.IsActive = Convert.ToByte(isActive);
        routeId.Replace(r);

        if (!isActive) return;
        var routes = GetList();
        foreach (var route in routes)
        {
            if (route != routeId)
            {
                var comp = route.Find<TradeRoute>();
                comp.IsActive = 0;
                route.Replace(comp);
            }
        }
    }

    public UniqueId GetActive()
    {
        var result = UniqueId.Nil;
        Entities.ForEach((ref UniqueId id,
                            ref TradeRoute comp) =>
        {
            if (comp.IsActive == 1)
            {
                result = id;
                return;
            }
        });
        return result;
    }
}
