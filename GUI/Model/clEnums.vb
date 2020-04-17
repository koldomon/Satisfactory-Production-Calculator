Imports System.ComponentModel
Imports System.Globalization

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
Public Class EnumBindingSourceExtension
    Inherits Markup.MarkupExtension


    Public Sub New()

    End Sub

    Public Sub New(thisType As Type)
        'If (thisType Is Nothing) Then Throw New ArgumentNullException(NameOf(thisType))
        Me._EnumType = thisType
    End Sub

    Private _EnumType As Type
    Public Property EnumType As Type
        Get
            Return _EnumType
        End Get
        Set
            If (Value IsNot Nothing) AndAlso (_EnumType IsNot Value) Then
                If (Not Value.IsEnum) Then Throw New ArgumentException("Type must be an Enum-Type!")

                _EnumType = Value
            End If
        End Set
    End Property

    Public Overrides Function ProvideValue(serviceProvider As IServiceProvider) As Object
        If (EnumType Is Nothing) Then Throw New ArgumentException("The Enum-Type must be specified!")

        Dim myType As Type = Nothing

        Dim myNullableType = Nullable.GetUnderlyingType(_EnumType)
        myType = If(myNullableType IsNot Nothing, myNullableType, _EnumType)

        Return [Enum].GetValues(myType)
    End Function
End Class
