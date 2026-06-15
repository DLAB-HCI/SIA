using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace EmbodiedNLI.Visualization
{
    public class FieldSpec
    {
        public string Field { get; set; }
        public string Type { get; set; }
        public ScaleSpec Scale { get; set; }
        public object Legend { get; set; }
        public object Sort { get; set; }
        public string Value { get; set; }
        public BinSpec Bin { get; set; }
        public AxisSpec Axis { get; set; }
        public string Aggregate { get; set; }
    }
    
    public class BinSpec
    {
        public bool Enable { get; set; }
        public int? Step { get; set; }
        public List<double> Edges { get; set; }
        public bool Normalize { get; set; } = true;
    }
    
    public class AxisSpec
    {
        public string Title { get; set; }
    }
    
    public class ScaleSpec
    {
        public string Type { get; set; }
        public JToken Domain { get; set; }

        public object Range { get; set; }
        public bool? Reverse { get; set; }
    }
    public class DataSpec
    {
        public string Url { get; set; }
    }

    public class TransformSpec
    {
        public string Filter { get; set; }
    }

    public class ColorConditionSpec
    {
        public string Test { get; set; }
        public string Value { get; set; }
    }

    public class ColorSpec
    {
        public string Field { get; set; }
        public string Type { get; set; }
        public ScaleSpec Scale { get; set; }
        public object Legend { get; set; }
        public object Sort { get; set; }
        public string Value { get; set; }
        public List<ColorConditionSpec> Condition { get; set; }
    }

    public class EncodingSpec
    {
        public FieldSpec X { get; set; }
        public FieldSpec Y { get; set; }
        public FieldSpec Z { get; set; }
        public ColorSpec Color { get; set; }
        public FieldSpec Detail { get; set; }
        public List<FieldSpec> Tooltip { get; set; }
    }

    public class ChartSpec
    {
        public string Mark { get; set; }
        public DataSpec Data { get; set; }
        public EncodingSpec Encoding { get; set; }
        public List<TransformSpec> Transform { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"[ChartSpec: Mark={Mark}, Fields={CountFields()}]";
        }

        private int CountFields()
        {
            int count = 0;
            if (Encoding?.X != null) count++;
            if (Encoding?.Y != null) count++;
            if (Encoding?.Z != null) count++;
            return count;
        }
    }

    public static class SpecAST
    {
        public static ChartSpec Parse(string json)
        {
            try
            {
                var root = JObject.Parse(json);

                return root.ToObject<ChartSpec>(JsonSerializer.Create(new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                }));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpecAST] JSON  : {ex.Message}");
                return null;
            }
        }
    }
}
