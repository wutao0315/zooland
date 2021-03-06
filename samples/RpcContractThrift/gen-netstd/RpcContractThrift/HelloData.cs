/**
 * Autogenerated by Thrift Compiler (0.14.2)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrift;
using Thrift.Collections;

using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Protocol.Utilities;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;
using Thrift.Processor;


#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE1006  // parts of the code use IDL spelling

namespace RpcContractThrift
{

  /// <summary>
  /// HelloResult实体
  /// </summary>
  public partial class HelloData : TBase
  {
    private string _Name;
    private string _Gender;
    private string _Head;

    public string Name
    {
      get
      {
        return _Name;
      }
      set
      {
        __isset.Name = true;
        this._Name = value;
      }
    }

    public string Gender
    {
      get
      {
        return _Gender;
      }
      set
      {
        __isset.Gender = true;
        this._Gender = value;
      }
    }

    public string Head
    {
      get
      {
        return _Head;
      }
      set
      {
        __isset.Head = true;
        this._Head = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool Name;
      public bool Gender;
      public bool Head;
    }

    public HelloData()
    {
    }

    public HelloData DeepCopy()
    {
      var tmp0 = new HelloData();
      if((Name != null) && __isset.Name)
      {
        tmp0.Name = this.Name;
      }
      tmp0.__isset.Name = this.__isset.Name;
      if((Gender != null) && __isset.Gender)
      {
        tmp0.Gender = this.Gender;
      }
      tmp0.__isset.Gender = this.__isset.Gender;
      if((Head != null) && __isset.Head)
      {
        tmp0.Head = this.Head;
      }
      tmp0.__isset.Head = this.__isset.Head;
      return tmp0;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        TField field;
        await iprot.ReadStructBeginAsync(cancellationToken);
        while (true)
        {
          field = await iprot.ReadFieldBeginAsync(cancellationToken);
          if (field.Type == TType.Stop)
          {
            break;
          }

          switch (field.ID)
          {
            case 1:
              if (field.Type == TType.String)
              {
                Name = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.String)
              {
                Gender = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.String)
              {
                Head = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            default: 
              await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              break;
          }

          await iprot.ReadFieldEndAsync(cancellationToken);
        }

        await iprot.ReadStructEndAsync(cancellationToken);
      }
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public async global::System.Threading.Tasks.Task WriteAsync(TProtocol oprot, CancellationToken cancellationToken)
    {
      oprot.IncrementRecursionDepth();
      try
      {
        var struc = new TStruct("HelloData");
        await oprot.WriteStructBeginAsync(struc, cancellationToken);
        var field = new TField();
        if((Name != null) && __isset.Name)
        {
          field.Name = "Name";
          field.Type = TType.String;
          field.ID = 1;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteStringAsync(Name, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Gender != null) && __isset.Gender)
        {
          field.Name = "Gender";
          field.Type = TType.String;
          field.ID = 2;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteStringAsync(Gender, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Head != null) && __isset.Head)
        {
          field.Name = "Head";
          field.Type = TType.String;
          field.ID = 3;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteStringAsync(Head, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        await oprot.WriteFieldStopAsync(cancellationToken);
        await oprot.WriteStructEndAsync(cancellationToken);
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override bool Equals(object that)
    {
      if (!(that is HelloData other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.Name == other.__isset.Name) && ((!__isset.Name) || (System.Object.Equals(Name, other.Name))))
        && ((__isset.Gender == other.__isset.Gender) && ((!__isset.Gender) || (System.Object.Equals(Gender, other.Gender))))
        && ((__isset.Head == other.__isset.Head) && ((!__isset.Head) || (System.Object.Equals(Head, other.Head))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if((Name != null) && __isset.Name)
        {
          hashcode = (hashcode * 397) + Name.GetHashCode();
        }
        if((Gender != null) && __isset.Gender)
        {
          hashcode = (hashcode * 397) + Gender.GetHashCode();
        }
        if((Head != null) && __isset.Head)
        {
          hashcode = (hashcode * 397) + Head.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var sb = new StringBuilder("HelloData(");
      int tmp1 = 0;
      if((Name != null) && __isset.Name)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Name: ");
        Name.ToString(sb);
      }
      if((Gender != null) && __isset.Gender)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Gender: ");
        Gender.ToString(sb);
      }
      if((Head != null) && __isset.Head)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Head: ");
        Head.ToString(sb);
      }
      sb.Append(')');
      return sb.ToString();
    }
  }

}
