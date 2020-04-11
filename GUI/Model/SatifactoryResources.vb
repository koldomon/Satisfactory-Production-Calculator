Imports System.Xml.Serialization

<XmlRoot(ElementName:="Satisfactory", [Namespace]:="")> Public Class SatifactoryResources
    <XmlArray(ElementName:="Recipes"), XmlArrayItem(Type:=GetType(Resource), ElementName:="Resource")> Public Property Recipes As New List(Of Resource)
End Class
