﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Core
{
    /// <summary>
    /// Plugin group base class for general settings
    /// </summary>
    [PluginGroupAllowMultipleEnabledPlugins(false)]
    [PluginGroupOrderIndex(1)]
    [PluginName("General Settings Plugins")]
    public abstract class PluginGroupGeneralSettings : Plugin
    {

    }
}
