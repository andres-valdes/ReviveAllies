using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
  public static class Collider_Extensions
  {
    public static T GetPreferredComponent<T>(this Collider collider)
    {
      T[] components = collider.GetComponents<T>();
      foreach (T component in components)
      {
        if (component is RevivePoint revivePoint)
        {
          if (revivePoint.IsValid())
          {
            return component;
          } else {
            ReviveAllies.logger.LogInfo("Found RevivePoint component, but it is not valid.");
          }
        }
      }
      return components.FirstOrDefault();
    }

    public static T GetPreferredComponentInParent<T>(this GameObject go)
    {
      T[] components = go.GetComponentsInParent<T>();
      foreach (T component in components)
      {
         if (component is RevivePoint revivePoint)
        {
          if (revivePoint.IsValid())
          {
            return component;
          } else {
            ReviveAllies.logger.LogInfo("Found RevivePoint parent component, but it is not valid.");
          }
        }
      }
      return components.FirstOrDefault();
    }
  }
}

