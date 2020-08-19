Imports System.ComponentModel
Imports System.Globalization

<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum BaseProductionEnum
    <Description("Default")> [Default] = 60
    <Description("Water")> Water = 120
    <Description("Crude Oil")> CrudeOil = 120
End Enum
<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum ResourceNodeTypeEnum
    <Description("Impure")> Impure = 30
    <Description("Normal")> Normal = 60
    <Description("Pure")> Pure = 120
End Enum

<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum MinerTypeEnum
    <Description("MK 1")> MK1 = 1
    <Description("MK 2")> MK2 = 2
    <Description("MK 3")> MK3 = 4
End Enum
<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum MinerSpeedEnum
    <Description("100 %")> Speed100 = 100
    <Description("150 %")> Speed150 = 150
    <Description("200 %")> Speed200 = 200
    <Description("250 %")> Speed250 = 250
End Enum
<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum BeltSpeedEnum
    <Description("Belt MK1")> MK1 = 60
    <Description("Belt MK2")> MK2 = 120
    <Description("Belt MK3")> MK3 = 270
    <Description("Belt MK4")> MK4 = 480
    <Description("Belt MK5")> MK5 = 780
    <Description("Belt MK6")> MK6 = 1200
End Enum

<TypeConverter(GetType(EnumTypeDescriptionConverter))> Public Enum CategoryEnum
    <Description("Undefined")> Undefined
    <Description("Item")> Item
    <Description("Basic Ore")> BasicOre
    <Description("Basic Fluid")> BasicFluid
    <Description("Basic Item")> BasicItem
    <Description("Fluid")> Fluid
    <Description("Ingot")> Ingot
    <Description("Collectable")> Collectable
End Enum
Public Class EnumTypeDescriptionConverter
    Inherits EnumConverter

    Public Sub New(thisType As Type)
        MyBase.New(thisType)
    End Sub

    Public Overrides Function ConvertTo(context As ITypeDescriptorContext, culture As CultureInfo, value As Object, destinationType As Type) As Object
        If (context Is Nothing) Then Throw New ArgumentNullException(NameOf(context))
        If (culture Is Nothing) Then Throw New ArgumentNullException(NameOf(culture))
        If (value Is Nothing) Then Throw New ArgumentNullException(NameOf(value))
        If (destinationType Is Nothing) Then Throw New ArgumentNullException(NameOf(destinationType))


        If (destinationType.Equals(GetType(String))) Then
            Dim myFieldInfo = value.GetType().GetField(value.ToString)
            If (myFieldInfo IsNot Nothing) Then
                Dim myDescription = myFieldInfo.GetCustomAttributes(GetType(DescriptionAttribute), False).Cast(Of DescriptionAttribute).FirstOrDefault
                If (myDescription IsNot Nothing) AndAlso (Not String.IsNullOrEmpty(myDescription.Description)) Then
                    Return myDescription.Description
                Else
                    Return String.Empty
                End If
            End If
        End If



        Return MyBase.ConvertTo(context, culture, value, destinationType)
    End Function
End Class
