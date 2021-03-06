﻿using Celestial.UIToolkit.Common;
using System;
using System.Windows.Media;
using static System.Math;

namespace Celestial.UIToolkit.Media.Animations
{

    internal sealed class BrushAnimationHelper : Singleton<BrushAnimationHelper>, IAnimationHelper<Brush>
    {

        private BrushAnimationHelper() { }

        public Brush GetZeroValue()
        {
            return new SolidColorBrush(new Color());
        }

        public Brush AddValues(Brush a, Brush b)
        {
            BrushAnimationInput transformedInput = BrushAnimationInputTransformer.Transform(a, b);
            a = transformedInput.From;
            b = transformedInput.To;

            return GetAnimationHelperForBrush(a)
                .AddValues(a, b);
        }

        public Brush SubtractValues(Brush a, Brush b)
        {
            BrushAnimationInput transformedInput = BrushAnimationInputTransformer.Transform(a, b);
            a = transformedInput.From;
            b = transformedInput.To;

            return GetAnimationHelperForBrush(a)
                .SubtractValues(a, b);
        }

        public Brush ScaleValue(Brush value, double factor)
        {
            value = BrushAnimationInputTransformer.Transform(value);

            return GetAnimationHelperForBrush(value)
                .ScaleValue(value, factor);
        }

        public Brush InterpolateValue(Brush from, Brush to, double progress)
        {
            BrushAnimationInput transformedInput = BrushAnimationInputTransformer.Transform(from, to);
            from = transformedInput.From;
            to = transformedInput.To;

            return GetAnimationHelperForBrush(from)
                .InterpolateValue(from, to, progress);
        }

        private static IAnimationHelper<Brush> GetAnimationHelperForBrush(Brush brush)
        {
            if (brush is SolidColorBrush)
                return SolidColorBrushAnimationHelper.Instance;
            else if (brush is LinearGradientBrush)
                return LinearGradientBrushAnimationHelper.Instance;
            else if (brush is RadialGradientBrush)
                return RadialGradientBrushAnimationHelper.Instance;
            else
                throw new NotImplementedException(
                    $"No animation helper exists for brush of type {brush.GetType().FullName}.");
        }



        /// <summary>
        /// A base class for an animation helper that animates a brush.
        /// This class already animates the brush's <see cref="Brush.Opacity"/>
        /// property.
        /// </summary>
        /// <typeparam name="TDeriving">
        /// The class deriving from this class.
        /// Used to setup a singleton pattern.
        /// </typeparam>
        /// <typeparam name="TBrush">
        /// The type of <see cref="Brush"/> which the deriving class is animating.
        /// </typeparam>
        private abstract class BrushAnimationHelperBase<TDeriving, TBrush> 
            : Singleton<TDeriving>, IAnimationHelper<Brush>
            where TBrush : Brush
        {
        
            private DoubleAnimationHelper _doubleHelper = DoubleAnimationHelper.Instance;

            public Brush GetZeroValue()
            {
                // This gets handled globally by the "BrushAnimationHelper" class.
                throw new NotImplementedException();
            }

            // For the animations to work, the brushes (each object really) must be cloned,
            // since working on reference types would yield incorrect results.
            // -> These methods clone the brushes, but delegate the actual value
            //    modification to the methods below.
            public Brush AddValues(Brush a, Brush b)
            {
                Brush result = a.Clone();
                AddValuesToResult((TBrush)result, (TBrush)a, (TBrush)b);
                return result;
            }

            public Brush SubtractValues(Brush a, Brush b)
            {
                Brush result = a.Clone();
                SubtractValuesFromResult((TBrush)result, (TBrush)a, (TBrush)b);
                return result;
            }

            public Brush ScaleValue(Brush value, double factor)
            {
                Brush result = value.Clone();
                ScaleResult((TBrush)result, factor);
                return result;
            }

            public Brush InterpolateValue(Brush from, Brush to, double progress)
            {
                Brush result = from.Clone();
                InterpolateResult((TBrush)result, (TBrush)from, (TBrush)to, progress);
                return result;
            }
        
            protected virtual void AddValuesToResult(TBrush result, TBrush a, TBrush b)
            {
                result.Opacity = _doubleHelper.AddValues(a.Opacity, b.Opacity);
            }
        
            protected virtual void SubtractValuesFromResult(TBrush result, TBrush a, TBrush b)
            {
                result.Opacity = _doubleHelper.SubtractValues(a.Opacity, b.Opacity);
            }
        
            protected virtual void ScaleResult(TBrush result, double factor)
            {
                result.Opacity = _doubleHelper.ScaleValue(result.Opacity, factor);
            }

            protected virtual void InterpolateResult(TBrush result, TBrush from, TBrush to, double progress)
            {
                result.Opacity = _doubleHelper.InterpolateValue(from.Opacity, to.Opacity, progress);
            }

        }

        private abstract class GradientBrushAnimationHelper<TDeriving, TBrush> 
            : BrushAnimationHelperBase<TDeriving, TBrush>
            where TBrush : GradientBrush
        {

            private DoubleAnimationHelper _doubleHelper = DoubleAnimationHelper.Instance;
            private ColorAnimationHelper _colorHelper = ColorAnimationHelper.Instance;

            protected override void AddValuesToResult(TBrush result, TBrush a, TBrush b)
            {
                base.AddValuesToResult(result, a, b);
                AddGradientStops(result.GradientStops, a.GradientStops, b.GradientStops);
            }

            protected override void SubtractValuesFromResult(TBrush result, TBrush a, TBrush b)
            {
                base.SubtractValuesFromResult(result, a, b);
                SubtractGradientStops(result.GradientStops, a.GradientStops, b.GradientStops);
            }

            protected override void ScaleResult(TBrush result, double factor)
            {
                base.ScaleResult(result, factor);
                ScaleGradientStops(result.GradientStops, factor);
            }

            protected override void InterpolateResult(TBrush result, TBrush from, TBrush to, double progress)
            {
                base.InterpolateResult(result, from, to, progress);
                InterpolateGradientStops(result.GradientStops, from.GradientStops, to.GradientStops, progress);
            }

            private void AddGradientStops(GradientStopCollection result, GradientStopCollection a, GradientStopCollection b)
            {
                for (int i = 0; i < Min(a.Count, b.Count); i++)
                {
                    GradientStop gradientStop = result[i];
                    gradientStop.Offset = _doubleHelper.AddValues(a[i].Offset, b[i].Offset);
                    gradientStop.Color = _colorHelper.AddValues(a[i].Color, b[i].Color);
                }
            }

            private void SubtractGradientStops(GradientStopCollection result, GradientStopCollection a, GradientStopCollection b)
            {
                for (int i = 0; i < Min(a.Count, b.Count); i++)
                {
                    GradientStop gradientStop = result[i];
                    gradientStop.Offset = _doubleHelper.SubtractValues(a[i].Offset, b[i].Offset);
                    gradientStop.Color = _colorHelper.SubtractValues(a[i].Color, b[i].Color);
                }
            }

            private void ScaleGradientStops(GradientStopCollection result, double factor)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    GradientStop gradientStop = result[i];
                    gradientStop.Offset = _doubleHelper.ScaleValue(result[i].Offset, factor);
                    gradientStop.Color = _colorHelper.ScaleValue(result[i].Color, factor);
                }
            }

            private void InterpolateGradientStops(
                GradientStopCollection result, GradientStopCollection from, GradientStopCollection to, double progress)
            {
                for (int i = 0; i < Min(from.Count, to.Count); i++)
                {
                    GradientStop gradientStop = result[i];
                    gradientStop.Offset = _doubleHelper.InterpolateValue(from[i].Offset, to[i].Offset, progress);
                    gradientStop.Color = _colorHelper.InterpolateValue(from[i].Color, to[i].Color, progress);
                }
            }
        
        }

        private sealed class SolidColorBrushAnimationHelper 
            : BrushAnimationHelperBase<SolidColorBrushAnimationHelper, SolidColorBrush>
        {

            private static ColorAnimationHelper _colorHelper = ColorAnimationHelper.Instance;

            public SolidColorBrushAnimationHelper() { }
        
            protected override void AddValuesToResult(SolidColorBrush result, SolidColorBrush a, SolidColorBrush b)
            {
                base.AddValuesToResult(result, a, b);
                result.Color = _colorHelper.AddValues(a.Color, b.Color);
            }

            protected override void SubtractValuesFromResult(SolidColorBrush result, SolidColorBrush a, SolidColorBrush b)
            {
                base.SubtractValuesFromResult(result, a, b);
                result.Color = _colorHelper.SubtractValues(a.Color, b.Color);
            }

            protected override void ScaleResult(SolidColorBrush result, double factor)
            {
                base.ScaleResult(result, factor);
                result.Color = _colorHelper.ScaleValue(result.Color, factor);
            }

            protected override void InterpolateResult(SolidColorBrush result, SolidColorBrush from, SolidColorBrush to, double progress)
            {
                base.InterpolateResult(result, from, to, progress);
                result.Color = _colorHelper.InterpolateValue(from.Color, to.Color, progress);
            }

        }

        private sealed class LinearGradientBrushAnimationHelper
            : GradientBrushAnimationHelper<LinearGradientBrushAnimationHelper, LinearGradientBrush>
        {

            private static PointAnimationHelper _pointHelper = PointAnimationHelper.Instance;
        
            protected override void AddValuesToResult(LinearGradientBrush result, LinearGradientBrush a, LinearGradientBrush b)
            {
                base.AddValuesToResult(result, a, b);
                result.StartPoint = _pointHelper.AddValues(a.StartPoint, b.StartPoint);
                result.EndPoint = _pointHelper.AddValues(a.EndPoint, b.EndPoint);
            }

            protected override void SubtractValuesFromResult(LinearGradientBrush result, LinearGradientBrush a, LinearGradientBrush b)
            {
                base.SubtractValuesFromResult(result, a, b);
                result.StartPoint = _pointHelper.SubtractValues(a.StartPoint, b.StartPoint);
                result.EndPoint = _pointHelper.SubtractValues(a.EndPoint, b.EndPoint);
            }

            protected override void ScaleResult(LinearGradientBrush result, double factor)
            {
                base.ScaleResult(result, factor);
                result.StartPoint = _pointHelper.ScaleValue(result.StartPoint, factor);
                result.EndPoint = _pointHelper.ScaleValue(result.EndPoint, factor);
            }

            protected override void InterpolateResult(LinearGradientBrush result, LinearGradientBrush from, LinearGradientBrush to, double progress)
            {
                base.InterpolateResult(result, from, to, progress);
                result.StartPoint = _pointHelper.InterpolateValue(from.StartPoint, to.StartPoint, progress);
                result.EndPoint = _pointHelper.InterpolateValue(from.EndPoint, to.EndPoint, progress);
            }

        }

        private sealed class RadialGradientBrushAnimationHelper
            : GradientBrushAnimationHelper<RadialGradientBrushAnimationHelper, RadialGradientBrush>
        {

            private static DoubleAnimationHelper _doubleHelper = DoubleAnimationHelper.Instance;
            private static PointAnimationHelper _pointHelper = PointAnimationHelper.Instance;
        
            protected override void AddValuesToResult(RadialGradientBrush result, RadialGradientBrush a, RadialGradientBrush b)
            {
                base.AddValuesToResult(result, a, b);
                result.RadiusX = _doubleHelper.AddValues(a.RadiusX, b.RadiusX);
                result.RadiusY = _doubleHelper.AddValues(a.RadiusY, b.RadiusY);
                result.Center = _pointHelper.AddValues(a.Center, b.Center);
                result.GradientOrigin = _pointHelper.AddValues(a.GradientOrigin, b.GradientOrigin);
            }

            protected override void SubtractValuesFromResult(RadialGradientBrush result, RadialGradientBrush a, RadialGradientBrush b)
            {
                base.SubtractValuesFromResult(result, a, b);
                result.RadiusX = _doubleHelper.SubtractValues(a.RadiusX, b.RadiusX);
                result.RadiusY = _doubleHelper.SubtractValues(a.RadiusY, b.RadiusY);
                result.Center = _pointHelper.SubtractValues(a.Center, b.Center);
                result.GradientOrigin = _pointHelper.SubtractValues(a.GradientOrigin, b.GradientOrigin);
            }

            protected override void ScaleResult(RadialGradientBrush result, double factor)
            {
                base.ScaleResult(result, factor);
                result.RadiusX = _doubleHelper.ScaleValue(result.RadiusX, factor);
                result.RadiusY = _doubleHelper.ScaleValue(result.RadiusY, factor);
                result.Center = _pointHelper.ScaleValue(result.Center, factor);
                result.GradientOrigin = _pointHelper.ScaleValue(result.GradientOrigin, factor);
            }

            protected override void InterpolateResult(RadialGradientBrush result, RadialGradientBrush from, RadialGradientBrush to, double progress)
            {
                base.InterpolateResult(result, from, to, progress);
                result.RadiusX = _doubleHelper.InterpolateValue(from.RadiusX, to.RadiusX, progress);
                result.RadiusY = _doubleHelper.InterpolateValue(from.RadiusY, to.RadiusY, progress);
                result.Center = _pointHelper.InterpolateValue(from.Center, to.Center, progress);
                result.GradientOrigin = _pointHelper.InterpolateValue(from.GradientOrigin, to.GradientOrigin, progress);
            }

        }

    }

}