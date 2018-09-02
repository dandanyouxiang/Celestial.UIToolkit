﻿using System.Windows;

namespace Celestial.UIToolkit.Controls
{

    /// <summary>
    /// A special type of shadow which aids in creating a unified shadow experience across the
    /// whole application by providing an elevation API to create layering and depth effects.
    /// 
    /// It can be attached to any type of controls via the <see cref="ShadowProperty"/>.
    /// For the shadow to be displayed, control template authors are required to display the
    /// <see cref="ThemeShadow"/> provided via the attached <see cref="ShadowProperty"/>.
    /// The easiest way to do this is by simply adding a <see cref="ThemeShadowHost"/> to the 
    /// control template.
    /// </summary>
    public partial class ThemeShadow : DependencyObject
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeShadow"/> class.
        /// </summary>
        public ThemeShadow()
        {
            UpdateCurrentDropShadowValues();
        }
        
        private static void ShadowElevation_Changed(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ThemeShadow)d;
            self.UpdateCurrentDropShadowValues();
        }

        private void UpdateCurrentDropShadowValues()
        {
            Vector shadowOffset = CalculateCurrentShadowOffset();

            BlurRadius = CalculateCurrentBlurRadius();
            OffsetX = shadowOffset.X;
            OffsetY = shadowOffset.Y;
        }

        /// <summary>
        /// Calculates a drop shadow's blur radius, based on the <see cref="ThemeShadow"/>'s 
        /// current <see cref="Elevation"/> and <see cref="ElevationShadowLengthMultiplier"/>
        /// properties.
        /// </summary>
        /// <returns>
        /// The blur radius of a drop shadow.
        /// </returns>
        protected virtual double CalculateCurrentBlurRadius()
        {
            return Elevation * ElevationShadowLengthMultiplier;
        }

        /// <summary>
        /// Calculates a drop shadow's offset in relation to the shadow-casting element.
        /// An offset of (0;0) means that the shadow is directly under the shadow-casting element.
        /// </summary>
        /// <returns>
        /// A <see cref="Vector"/> which represents the drop shadow's offset.
        /// By default, this returns (0;0).
        /// </returns>
        protected virtual Vector CalculateCurrentShadowOffset()
        {
            // The default ThemeShadow is ambient - it draws the shadow in neither direction.
            return new Vector(0d, 0d);
        }

        /// <summary>
        /// Returns a string representation of the <see cref="ThemeShadow"/>.
        /// </summary>
        /// <returns>A string representing the <see cref="ThemeShadow"/>.</returns>
        public override string ToString()
        {
            return $"{nameof(ThemeShadow)}: " +
                   $"{nameof(Elevation)}: {Elevation}";
        }

    }
    
}
