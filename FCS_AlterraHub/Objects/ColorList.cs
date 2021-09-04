﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FCS_AlterraHub.Extensions;
using FCSCommon.Utilities;
using UnityEngine;

namespace FCS_AlterraHub.Objects
{
    public static class ColorList
    {
        private static object[] _keys;

        private static readonly OrderedDictionary Colors = new OrderedDictionary
        {
            {new Vec4(0f,0f,0f,1f), "Black"},
            {new Vec4(1f,1f,1f,1f),"White"},
            {new Vec4(0.7529412f, 0.7529412f, 0.7529412f, 1f),"Silver"},
            {new Vec4(1f,0f,0f,1f),"Red"},
            {new Vec4(1f, 0.4980392f, 0f,1f), "Orange"},
            {new Vec4(1f, 1f, 0f,1f), "Yellow"},
            {new Vec4(0.5019608f, 1f, 0f,1f), "Lime Green"},
            {new Vec4(0f, 1f, 0f,1f),"Green"},
            {new Vec4(0f, 1f, 1f,1f), "Aqua"},
            {new Vec4(0f, 0.5019608f, 1f,1f), "Dodger Blue"},
            {new Vec4(0f,0f,1f,1f), "Blue"},
            {new Vec4(0.5019608f,0f,1f,1f), "Electric Indigo"},
            {new Vec4(1f,0f,1f,1f),"Magenta"},
            {new Vec4(0.9960785f,0f,0.4980392f,1f), "Deep Pink"},
            {new Vec4(0.8941177f,0.6784314f,0.01960784f,1f), "Gamboge"},
            {new Vec4(0.1882353f,0.1843137f,0.1803922f,1f), "Acadia"},
            {new Vec4(0.4941177f,0.4941177f,0.4941177f,1f), "Grey"},
            {new Vec4(0.3333333f,0.3333333f,0.3333333f,1f), "Matterhorn"},
        };

        public static List<Vec4> OldColorsList = new List<Vec4>
        {
            new Vec4(0f,0f,0f,1f),
            new Vec4(0.827451f,1f,0.9921569f,1f),
            new Vec4(0.9568628f,0.764706f,0f,1f),
            new Vec4(0.8666667f,0.1960784f,0.1254902f,1f),
            new Vec4(1f,1f,1f,1f),
            new Vec4(0.2784314f,0.8509805f,0f,1f),
            new Vec4(0f,0.3215686f,0.8509805f,1f),
            new Vec4(0.509804f,0.01568628f,0.9333334f,1f),
            new Vec4(0.8039216f,0.3607843f,0.3607843f,1f),
            new Vec4(0.9411765f,0.5019608f,0.5019608f,1f),
            new Vec4(0.9803922f,0.5019608f,0.4470588f,1f),
            new Vec4(0.9137255f,0.5882353f,0.4784314f,1f),
            new Vec4(1f,0.627451f,0.4784314f,1f),
            new Vec4(0.8627451f,0.07843138f,0.2352941f,1f),
            new Vec4(1f,0f,0f,1f),
            new Vec4(0.6980392f,0.1333333f,0.1333333f,1f),
            new Vec4(0.5450981f,0f,0f,1f),
            new Vec4(1f,0.7529412f,0.7960784f,1f),
            new Vec4(1f,0.7137255f,0.7568628f,1f),
            new Vec4(1f,0.4117647f,0.7058824f,1f),
            new Vec4(1f,0.07843138f,0.5764706f,1f),
            new Vec4(0.7803922f,0.08235294f,0.5215687f,1f),
            new Vec4(0.8588235f,0.4392157f,0.5764706f,1f),
            new Vec4(1f,0.627451f,0.4784314f,1f),
            new Vec4(1f,0.4980392f,0.3137255f,1f),
            new Vec4(1f,0.3882353f,0.2784314f,1f),
            new Vec4(1f,0.2705882f,0f,1f),
            new Vec4(1f,0.5490196f,0f,1f),
            new Vec4(1f,0.6470588f,0f,1f),
            new Vec4(1f,0.8431373f,0f,1f),
            new Vec4(1f,1f,0f,1f),
            new Vec4(1f,1f,0.8784314f,1f),
            new Vec4(1f,0.9803922f,0.8039216f,1f),
            new Vec4(0.9803922f,0.9803922f,0.8235294f,1f),
            new Vec4(1f,0.9372549f,0.8352941f,1f),
            new Vec4(1f,0.8941177f,0.7098039f,1f),
            new Vec4(1f,0.854902f,0.7254902f,1f),
            new Vec4(0.9333333f,0.9098039f,0.6666667f,1f),
            new Vec4(0.9411765f,0.9019608f,0.5490196f,1f),
            new Vec4(0.7411765f,0.7176471f,0.4196078f,1f),
            new Vec4(0.9019608f,0.9019608f,0.9803922f,1f),
            new Vec4(0.8470588f,0.7490196f,0.8470588f,1f),
            new Vec4(0.8666667f,0.627451f,0.8666667f,1f),
            new Vec4(0.9333333f,0.509804f,0.9333333f,1f),
            new Vec4(0.854902f,0.4392157f,0.8392157f,1f),
            new Vec4(1f,0f,1f,1f),
            new Vec4(1f,0f,1f,1f),
            new Vec4(0.7294118f,0.3333333f,0.827451f,1f),
            new Vec4(0.5764706f,0.4392157f,0.8588235f,1f),
            new Vec4(0.4f,0.2f,0.6f,1f),
            new Vec4(0.5411765f,0.1686275f,0.8862745f,1f),
            new Vec4(0.5803922f,0f,0.827451f,1f),
            new Vec4(0.6f,0.1960784f,0.8f,1f),
            new Vec4(0.5450981f,0f,0.5450981f,1f),
            new Vec4(0.5019608f,0f,0.5019608f,1f),
            new Vec4(0.2941177f,0f,0.509804f,1f),
            new Vec4(0.4156863f,0.3529412f,0.8039216f,1f),
            new Vec4(0.282353f,0.2392157f,0.5450981f,1f),
            new Vec4(0.4823529f,0.4078431f,0.9333333f,1f),
            new Vec4(0.6784314f,1f,0.1843137f,1f),
            new Vec4(0.4980392f,1f,0f,1f),
            new Vec4(0.4862745f,0.9882353f,0f,1f),
            new Vec4(0f,1f,0f,1f),
            new Vec4(0.1960784f,0.8039216f,0.1960784f,1f),
            new Vec4(0.5960785f,0.9843137f,0.5960785f,1f),
            new Vec4(0.5647059f,0.9333333f,0.5647059f,1f),
            new Vec4(0f,0.9803922f,0.6039216f,1f),
            new Vec4(0f,1f,0.4980392f,1f),
            new Vec4(0.2352941f,0.7019608f,0.4431373f,1f),
            new Vec4(0.1803922f,0.5450981f,0.3411765f,1f),
            new Vec4(0.1333333f,0.5450981f,0.1333333f,1f),
            new Vec4(0f,0.5019608f,0f,1f),
            new Vec4(0f,0.3921569f,0f,1f),
            new Vec4(0.6039216f,0.8039216f,0.1960784f,1f),
            new Vec4(0.4196078f,0.5568628f,0.1372549f,1f),
            new Vec4(0.5019608f,0.5019608f,0f,1f),
            new Vec4(0.3333333f,0.4196078f,0.1843137f,1f),
            new Vec4(0.4f,0.8039216f,0.6666667f,1f),
            new Vec4(0.5607843f,0.7372549f,0.5450981f,1f),
            new Vec4(0.1254902f,0.6980392f,0.6666667f,1f),
            new Vec4(0f,0.5450981f,0.5450981f,1f),
            new Vec4(0f,0.5019608f,0.5019608f,1f),
            new Vec4(0f,1f,1f,1f),
            new Vec4(0f,1f,1f,1f),
            new Vec4(0.8784314f,1f,1f,1f),
            new Vec4(0.6862745f,0.9333333f,0.9333333f,1f),
            new Vec4(0.4980392f,1f,0.8313726f,1f),
            new Vec4(0.2509804f,0.8784314f,0.8156863f,1f),
            new Vec4(0.282353f,0.8196079f,0.8f,1f),
            new Vec4(0f,0.8078431f,0.8196079f,1f),
            new Vec4(0.372549f,0.6196079f,0.627451f,1f),
            new Vec4(0.2745098f,0.509804f,0.7058824f,1f),
            new Vec4(0.6901961f,0.7686275f,0.8705882f,1f),
            new Vec4(0.6901961f,0.8784314f,0.9019608f,1f),
            new Vec4(0.6784314f,0.8470588f,0.9019608f,1f),
            new Vec4(0.5294118f,0.8078431f,0.9215686f,1f),
            new Vec4(0.5294118f,0.8078431f,0.9803922f,1f),
            new Vec4(0f,0.7490196f,1f,1f),
            new Vec4(0.1176471f,0.5647059f,1f,1f),
            new Vec4(0.3921569f,0.5843138f,0.9294118f,1f),
            new Vec4(0.4823529f,0.4078431f,0.9333333f,1f),
            new Vec4(0.254902f,0.4117647f,0.8823529f,1f),
            new Vec4(0f,0f,1f,1f),
            new Vec4(0f,0f,0.8039216f,1f),
            new Vec4(0f,0f,0.5450981f,1f),
            new Vec4(0f,0f,0.5019608f,1f),
            new Vec4(0.09803922f,0.09803922f,0.4392157f,1f),
            new Vec4(1f,0.972549f,0.8627451f,1f),
            new Vec4(1f,0.9215686f,0.8039216f,1f),
            new Vec4(1f,0.8941177f,0.7686275f,1f),
            new Vec4(1f,0.8705882f,0.6784314f,1f),
            new Vec4(0.9607843f,0.8705882f,0.7019608f,1f),
            new Vec4(0.8705882f,0.7215686f,0.5294118f,1f),
            new Vec4(0.8235294f,0.7058824f,0.5490196f,1f),
            new Vec4(0.7372549f,0.5607843f,0.5607843f,1f),
            new Vec4(0.9568627f,0.6431373f,0.3764706f,1f),
            new Vec4(0.854902f,0.6470588f,0.1254902f,1f),
            new Vec4(0.7215686f,0.5254902f,0.04313726f,1f),
            new Vec4(0.8039216f,0.5215687f,0.2470588f,1f),
            new Vec4(0.8235294f,0.4117647f,0.1176471f,1f),
            new Vec4(0.5450981f,0.2705882f,0.07450981f,1f),
            new Vec4(0.627451f,0.3215686f,0.1764706f,1f),
            new Vec4(0.6470588f,0.1647059f,0.1647059f,1f),
            new Vec4(0.5019608f,0f,0f,1f),
            new Vec4(1f,0.9803922f,0.9803922f,1f),
            new Vec4(0.9411765f,1f,0.9411765f,1f),
            new Vec4(0.7529412f,0.7529412f,0.7529412f,1f),
            new Vec4(0.6627451f,0.6627451f,0.6627451f,1f),
            new Vec4(0.5019608f,0.5019608f,0.5019608f,1f),
            new Vec4(0.4117647f,0.4117647f,0.4117647f,1f),
            new Vec4(0.4666667f,0.5333334f,0.6f,1f),
            new Vec4(0.4392157f,0.5019608f,0.5647059f,1f),
            new Vec4(0.1843137f,0.3098039f,0.3098039f,1f),
            new Vec4(0.3f,0.3f,0.3f,1f),
        };
        
        public static void AddColor(Color color, string colorName = "N/A")
        {
            var vec4Color = color.ColorToVector4();

            foreach (DictionaryEntry entry in Colors)
            {
                if ((string)entry.Value == colorName) return;
            }

            Colors.Add(vec4Color, colorName);
            QuickLogger.Info($"Added new color to ColorsList: {color}");
        }

        public static void AddColor(Vec4 color, string colorName = "N/A")
        {
            var vec4Color = ConvertColor(color);

            foreach (DictionaryEntry entry in Colors)
            {
                if ((string)entry.Value == colorName) return;
            }

            Colors.Add(vec4Color, colorName);
            QuickLogger.Info($"Added new color to ColorsList: {color}");
        }

        public static Vec4 ConvertColor(Vec4 color)
        {
            return new Vec4(color.R/255.0f, color.G/ 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

    public static string GetName(Color color)
        {
            var vecColor = color.ColorToVector4();
            return Colors.Contains(vecColor) ? (string)Colors[vecColor] : "Unknown Color";
        }

        public static int GetColorIndex(Color currentColor)
        {
            GetKeys();
            for (int i = 0; i < Colors.Keys.Count; i++)
            {
#if DEBUG
                QuickLogger.Debug($"Index = {i}, Key = {_keys[i]}, Value = {Colors[i]}");
#endif
                if((Vec4)_keys[i] == currentColor.ColorToVector4())
                    return i;
            }

            return 0;
        }

        private static void GetKeys()
        {
            if (_keys == null)
            {
                _keys = new object[Colors.Keys.Count];
                Colors.Keys.CopyTo(_keys, 0);
            }
        }

        public static int GetCount()
        {
            return Colors.Count;
        }

        public static Color GetColor(int currentIndex)
        {
            GetKeys();
            return ((Vec4)_keys[currentIndex]).Vector4ToColor();
        }

        public static bool Contains(Vec4 currentVec4)
        {
            return Colors.Contains(currentVec4);
        }

        public static string GetName(Vec4 currentVec4)
        {
            return (string)Colors[currentVec4];
        }

    }
}
