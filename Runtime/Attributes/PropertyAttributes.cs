﻿using System;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 属性用于对象或字符串字段，如果未设置，则将其标记为必需(红色)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiredAttribute : PropertyAttribute
    {
    }
}