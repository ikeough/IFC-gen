

using System;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using STEP;
	
namespace IFC
{
	/// <summary>
	/// <see href="http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifczshapeprofiledef.htm"/>
	/// </summary>
	public  partial class IfcZShapeProfileDef : IfcParameterizedProfileDef
	{
		public IfcPositiveLengthMeasure Depth{get;set;} 
		public IfcPositiveLengthMeasure FlangeWidth{get;set;} 
		public IfcPositiveLengthMeasure WebThickness{get;set;} 
		public IfcPositiveLengthMeasure FlangeThickness{get;set;} 
		public IfcNonNegativeLengthMeasure FilletRadius{get;set;} // optional
		public IfcNonNegativeLengthMeasure EdgeRadius{get;set;} // optional


		/// <summary>
		/// Construct a IfcZShapeProfileDef with all required attributes.
		/// </summary>
		public IfcZShapeProfileDef(IfcProfileTypeEnum profileType,IfcPositiveLengthMeasure depth,IfcPositiveLengthMeasure flangeWidth,IfcPositiveLengthMeasure webThickness,IfcPositiveLengthMeasure flangeThickness):base(profileType)
		{

			Depth = depth;
			FlangeWidth = flangeWidth;
			WebThickness = webThickness;
			FlangeThickness = flangeThickness;

		}
		/// <summary>
		/// Construct a IfcZShapeProfileDef with required and optional attributes.
		/// </summary>
		[JsonConstructor]
		public IfcZShapeProfileDef(IfcProfileTypeEnum profileType,IfcLabel profileName,IfcAxis2Placement2D position,IfcPositiveLengthMeasure depth,IfcPositiveLengthMeasure flangeWidth,IfcPositiveLengthMeasure webThickness,IfcPositiveLengthMeasure flangeThickness,IfcNonNegativeLengthMeasure filletRadius,IfcNonNegativeLengthMeasure edgeRadius):base(profileType,profileName,position)
		{

			Depth = depth;
			FlangeWidth = flangeWidth;
			WebThickness = webThickness;
			FlangeThickness = flangeThickness;
			FilletRadius = filletRadius;
			EdgeRadius = edgeRadius;

		}
		public static new IfcZShapeProfileDef FromJSON(string json){ return JsonConvert.DeserializeObject<IfcZShapeProfileDef>(json); }

        public override string GetStepParameters()
        {
            var parameters = new List<string>();
			parameters.Add(ProfileType.ToStepValue());
			parameters.Add(ProfileName != null ? ProfileName.ToStepValue() : "$");
			parameters.Add(Position != null ? Position.ToStepValue() : "$");
			parameters.Add(Depth != null ? Depth.ToStepValue() : "$");
			parameters.Add(FlangeWidth != null ? FlangeWidth.ToStepValue() : "$");
			parameters.Add(WebThickness != null ? WebThickness.ToStepValue() : "$");
			parameters.Add(FlangeThickness != null ? FlangeThickness.ToStepValue() : "$");
			parameters.Add(FilletRadius != null ? FilletRadius.ToStepValue() : "$");
			parameters.Add(EdgeRadius != null ? EdgeRadius.ToStepValue() : "$");

            return string.Join(", ", parameters.ToArray());
        }
	}
}
