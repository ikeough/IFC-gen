
import {BaseIfc} from "./BaseIfc"
import {IfcRelAssociatesMaterial} from "./IfcRelAssociatesMaterial.g"
import {IfcExternalReferenceRelationship} from "./IfcExternalReferenceRelationship.g"
import {IfcMaterialProperties} from "./IfcMaterialProperties.g"

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifcmaterialdefinition.htm
 */
export abstract class IfcMaterialDefinition extends BaseIfc {
	AssociatedTo : Array<IfcRelAssociatesMaterial> // inverse
	HasExternalReferences : Array<IfcExternalReferenceRelationship> // inverse
	HasProperties : Array<IfcMaterialProperties> // inverse

    constructor() {
        super()
    }
}