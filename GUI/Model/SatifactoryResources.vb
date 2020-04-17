Imports System.Xml.Serialization

<XmlRoot(ElementName:="Satisfactory", [Namespace]:="")> Public Class SatifactoryResources
    <XmlArray(ElementName:="Recipes"), XmlArrayItem(Type:=GetType(Resource), ElementName:="Resource")> Public Property Recipes As New List(Of Resource)
    <XmlArray(ElementName:="Productions"), XmlArrayItem(Type:=GetType(ProductionItem), ElementName:="Production")> Public Property Productions As New List(Of ProductionItem)
End Class
