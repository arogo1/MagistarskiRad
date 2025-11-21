namespace Common;

public sealed class PQHandle<T>
{
    public object? NodeRef; // konkretni heap će znati šta ovdje čuva
    public PQHandle(object? nodeRef) => NodeRef = nodeRef;
}