﻿using System;
using Qualia.Controls.Base;
using Qualia.Tools;
using Qualia.Tools.Managers;

namespace Qualia.Network;

public sealed partial class SettingsControl : BaseUserControl
{
    public Settings Settings;

    public SettingsControl()
        : base(0)
    {
        InitializeComponent();

        this.SetConfigParams(new()
        {
            CtlSkipRoundsToDrawErrorMatrix
                .Initialize(defaultValue: 10000)
                .SetUIParam(Notification.ParameterChanged.Settings),

            CtlSkipRoundsToDrawNetworks
                .Initialize(defaultValue: 10000)
                .SetUIParam(Notification.ParameterChanged.Settings),

            CtlSkipRoundsToDrawStatistics
                .Initialize(defaultValue: 10000)
                .SetUIParam(Notification.ParameterChanged.Settings),

            CtlIsNoSleepMode
                .Initialize(defaultValue: true)
                .SetUIParam(Notification.ParameterChanged.NoSleepMode)
        });
    }

    public override void SetOnChangeEvent(ActionManager.ApplyActionDelegate onChanged)
    {
        this.SetUIHandler(onChanged);
        this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Value_OnChanged));

        ApplyChanges(false);
        Value_OnChanged(new(this, Notification.ParameterChanged.NoSleepMode));
    }

    private void Value_OnChanged(ApplyAction action)
    {
        var param = action.Param;

        if (param == Notification.ParameterChanged.Settings
            || param == Notification.ParameterChanged.NoSleepMode)
        {
            OnChanged(action);
        }
        else
        {
            throw new ArgumentException(param.ToString());
        }
    }

    private Settings Get()
    {
        return new()
        {
            SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
            SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
            SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value,
            IsNoSleepMode = CtlIsNoSleepMode.Value
        };
    }

    public void ApplyChanges(bool isRunning)
    {
        Settings = Get();
    }
}

public sealed class Settings
{
    public int SkipRoundsToDrawErrorMatrix;
    public int SkipRoundsToDrawNetworks;
    public int SkipRoundsToDrawStatistics;
    public bool IsNoSleepMode;
}