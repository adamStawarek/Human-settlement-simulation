﻿using SettlementSimulation.Engine.Interfaces;
using System;
using System.Linq;
using System.Reflection;

namespace SettlementSimulation.Engine.Helpers
{
    public class RuleDistributor : IRuleDistributor
    {
        public IRule GetRule<T>(params object[] parameters) where T : IRule
        {
            return (IRule)Assembly.Load("SettlementSimulation.Engine")
                .GetTypes()?
                .Where(t => t == typeof(T))
                .Select(t => Activator.CreateInstance(t, parameters))
                .FirstOrDefault();
        }
    }
}
