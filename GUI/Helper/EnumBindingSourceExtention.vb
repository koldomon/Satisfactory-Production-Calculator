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
