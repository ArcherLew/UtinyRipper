﻿using System;
using System.Collections.Generic;
using UtinyRipper.AssetExporters;
using UtinyRipper.Classes.GameObjects;
using UtinyRipper.Exporter.YAML;
using UtinyRipper.SerializedFiles;

namespace UtinyRipper.Classes
{
	public sealed class GameObject : EditorExtension
	{
		public GameObject(AssetInfo assetInfo):
			base(assetInfo)
		{
		}

		private static void CollectHierarchy(GameObject root, List<EditorExtension> heirarchy)
		{
			heirarchy.Add(root);

			Transform transform = null;
			foreach (ComponentPair cpair in root.Components)
			{
				Component component = cpair.Component.FindObject(root.File);
				if(component == null)
				{
					continue;
				}

				heirarchy.Add(component);
				if (component.ClassID.IsTransform())
				{
					transform = (Transform)component;
				}
			}

			foreach (PPtr<Transform> pchild in transform.Children)
			{
				Transform child = pchild.GetObject(root.File);
				GameObject childGO = child.GameObject.GetObject(root.File);
				CollectHierarchy(childGO, heirarchy);
			}
		}

		/// <summary>
		/// Less than 4.0.0
		/// In earlier versions GameObjects always has IsActive as false.
		/// </summary>
		private static bool IsAlwaysDeactivated(Version version)
		{
#warning unknown
			return version.IsLess(4);
		}

		private static int GetSerializedVersion(Version version)
		{
#warning TODO: serialized version acording to read version (current 2017.3.0f3)
			return 5;
		}

		public override void Read(AssetStream stream)
		{
			base.Read(stream);

			Components = stream.ReadArray<ComponentPair>();

			Layer = stream.ReadInt32();
			Name = stream.ReadStringAligned();
			Tag = stream.ReadUInt16();
			IsActive = stream.ReadBoolean();
		}
		
		public override IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			foreach (Object @object in base.FetchDependencies(file, isLog))
			{
				yield return @object;
			}
			foreach(ComponentPair pair in Components)
			{
				foreach (Object @object in pair.FetchDependencies(file, isLog))
				{
					yield return @object;
				}
			}
		}

		public GameObject GetRoot()
		{
			Transform root = GetTransform();
			while (true)
			{
				Transform parent = root.Father.TryGetObject(File);
				if (parent == null)
				{
					break;
				}
				else
				{
					root = parent;
				}
			}
			return root.GameObject.GetObject(File);
		}

		public int GetRootDepth()
		{
			Transform root = GetTransform();
			int depth = 0;
			while (true)
			{
				Transform parent = root.Father.TryGetObject(File);
				if (parent == null)
				{
					break;
				}

				root = parent;
				depth++;
			}
			return depth;
		}

		protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)
		{
#warning TODO: values acording to read version (current 2017.3.0f3)
			YAMLMappingNode node = base.ExportYAMLRoot(container);
			node.AddSerializedVersion(GetSerializedVersion(container.Version));
			node.Add("m_Component", Components.ExportYAML(container));
			node.Add("m_Layer", Layer);
			node.Add("m_Name", Name);
#warning TODO: tag index to string name
			node.Add("m_TagString", "Untagged");
			node.Add("m_Icon", default(PPtr<Object>).ExportYAML(container));
			node.Add("m_NavMeshLayer", 0);
			node.Add("m_StaticEditorFlags", 0);
			node.Add("m_IsActive", GetExportIsActive(container.Version));
			return node;
		}

		public Transform GetTransform()
		{
			foreach (ComponentPair pair in Components)
			{
				Component comp = pair.Component.FindObject(File);
				if (comp == null)
				{
					continue;
				}

				if (comp.ClassID.IsTransform())
				{
					return (Transform)comp;
				}
			}
			return null;
		}
		
		public List<EditorExtension> CollectHierarchy()
		{
			List<EditorExtension> heirarchy = new List<EditorExtension>();
			CollectHierarchy(this, heirarchy);
			return heirarchy;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Name))
			{
				return base.ToString();
			}
			return $"{Name}({GetType().Name})";
		}

		private bool GetExportIsActive(Version version)
		{
#warning TODO: fix
			return IsAlwaysDeactivated(version) ? true : IsActive;
		}

		public override string ExportExtension => throw new NotSupportedException();
		
		public ComponentPair[] Components { get; private set; }
		public int Layer { get; private set; }
		public string Name { get; private set; } = string.Empty;
		public ushort Tag { get; private set; }
		public bool IsActive { get; private set; }
	}
}
