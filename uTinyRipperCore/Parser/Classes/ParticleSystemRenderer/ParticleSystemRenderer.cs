﻿using System;
using System.Collections;
using System.Collections.Generic;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes.ParticleSystemRenderers;
using uTinyRipper.Classes.SpriteRenderers;
using uTinyRipper.Exporter.YAML;
using uTinyRipper.SerializedFiles;

namespace uTinyRipper.Classes
{
	public sealed class ParticleSystemRenderer : Renderer
	{
		public ParticleSystemRenderer(AssetInfo assetInfo):
			base(assetInfo)
		{
		}

		/// <summary>
		/// 5.3.0 and greater
		/// </summary>
		public static bool IsReadMinParticleSize(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}
		/// <summary>
		/// 4.0.0 and greater
		/// </summary>
		public static bool IsReadNormalDirection(Version version)
		{
			return version.IsGreaterEqual(4);
		}
		/// <summary>
		/// 5.3.0 and greater
		/// </summary>
		public static bool IsReadRenderAlignment(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}
		/// <summary>
		/// 5.5.0 and greater
		/// </summary>
		public static bool IsReadUseCustomVertexStreams(Version version)
		{
			return version.IsGreaterEqual(5, 5);
		}
		/// <summary>
		/// 5.5.0 to 5.6.0 exclusive
		/// </summary>
		public static bool IsReadVertexStreamMask(Version version)
		{
			return version.IsGreaterEqual(5, 5) && version.IsLess(5, 6);
		}
		
		/// <summary>
		/// 4.0.0 and greater
		/// </summary>
		public static bool IsReadMeshes(Version version)
		{
			return version.IsGreaterEqual(4);
		}
		/// <summary>
		/// 2017.1.0b2 and greater
		/// </summary>
		public static bool IsReadMaskInteraction(Version version)
		{
			return version.IsGreaterEqual(2017, 1, 0, VersionType.Beta, 2);
		}
		
		/// <summary>
		/// 5.3.0 and greater
		/// </summary>
		private static bool IsModeShort(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}
		/// <summary>
		/// 5.3.0 and greater
		/// </summary>
		private static bool IsSortModeFirst(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}

		private static int GetSerializedVersion(Version version)
		{
			if (Config.IsExportTopmostSerializedVersion)
			{
				return 4;
			}

			if (version.IsGreaterEqual(2017, 1, 0, VersionType.Beta, 2))
			{
				return 4;
			}
			if (version.IsGreaterEqual(5, 6))
			{
				return 3;
			}
			if (version.IsGreaterEqual(5, 5))
			{
				return 2;
			}
			return 1;
		}

		public override void Read(AssetReader reader)
		{
			base.Read(reader);

			RenderMode = IsModeShort(reader.Version) ? (ParticleSystemRenderMode)reader.ReadUInt16() : (ParticleSystemRenderMode)reader.ReadInt32();
			if (IsSortModeFirst(reader.Version))
			{
				SortMode = (ParticleSystemSortMode)reader.ReadUInt16();
			}

			if (IsReadMinParticleSize(reader.Version))
			{
				MinParticleSize = reader.ReadSingle();
			}
			MaxParticleSize = reader.ReadSingle();
			CameraVelocityScale = reader.ReadSingle();
			VelocityScale = reader.ReadSingle();
			LengthScale = reader.ReadSingle();
			SortingFudge = reader.ReadSingle();

			if (IsReadNormalDirection(reader.Version))
			{
				NormalDirection = reader.ReadSingle();
			}
			if (!IsSortModeFirst(reader.Version))
			{
				SortMode = (ParticleSystemSortMode)reader.ReadInt32();
			}

			if (IsReadRenderAlignment(reader.Version))
			{
				RenderAlignment = (ParticleSystemRenderSpace)reader.ReadInt32();
				Pivot.Read(reader);
			}
			else
			{
				RenderAlignment = RenderMode == ParticleSystemRenderMode.Mesh ? ParticleSystemRenderSpace.Local : ParticleSystemRenderSpace.View;
			}

			if (IsReadUseCustomVertexStreams(reader.Version))
			{
				UseCustomVertexStreams = reader.ReadBoolean();
				reader.AlignStream(AlignType.Align4);

				if (IsReadVertexStreamMask(reader.Version))
				{
					int vertexStreamMask = reader.ReadInt32();
					List<byte> vertexStreams = new List<byte>(8);
					for(byte i = 0; i < 8; i++)
					{
						if((vertexStreamMask & (1 << i)) != 0)
						{
							vertexStreams.Add(i);
						}
					}
					m_vertexStreams = vertexStreams.ToArray();
				}
				else
				{
					m_vertexStreams = reader.ReadByteArray();
					reader.AlignStream(AlignType.Align4);
				}
			}

			Mesh.Read(reader);
			if (IsReadMeshes(reader.Version))
			{
				Mesh1.Read(reader);
				Mesh2.Read(reader);
				Mesh3.Read(reader);
			}
			if (IsReadMaskInteraction(reader.Version))
			{
				MaskInteraction = (SpriteMaskInteraction)reader.ReadInt32();
			}
		}

		public override IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			foreach(Object asset in base.FetchDependencies(file, isLog))
			{
				yield return asset;
			}
			
			yield return Mesh.FetchDependency(file, isLog, ToLogString, "m_Mesh");
			if (IsReadMeshes(file.Version))
			{
				yield return Mesh1.FetchDependency(file, isLog, ToLogString, "m_Mesh1");
				yield return Mesh2.FetchDependency(file, isLog, ToLogString, "m_Mesh2");
				yield return Mesh3.FetchDependency(file, isLog, ToLogString, "m_Mesh3");
			}
		}

		protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)
		{
			YAMLMappingNode node = base.ExportYAMLRoot(container);
			node.InsertSerializedVersion(GetSerializedVersion(container.Version));
			node.Add("m_RenderMode", (short)RenderMode);
			node.Add("m_SortMode", (short)SortMode);
			node.Add("m_MinParticleSize", MinParticleSize);
			node.Add("m_MaxParticleSize", MaxParticleSize);
			node.Add("m_CameraVelocityScale", CameraVelocityScale);
			node.Add("m_VelocityScale", VelocityScale);
			node.Add("m_LengthScale", LengthScale);
			node.Add("m_SortingFudge", SortingFudge);
			node.Add("m_NormalDirection", GetNormalDirection(container.Version));
			node.Add("m_RenderAlignment", (int)RenderAlignment);
			node.Add("m_Pivot", Pivot.ExportYAML(container));
			node.Add("m_UseCustomVertexStreams", UseCustomVertexStreams);
			node.Add("m_VertexStreams", GetVertexStreams(container.Version).ExportYAML());
			node.Add("m_Mesh", Mesh.ExportYAML(container));
			node.Add("m_Mesh1", Mesh1.ExportYAML(container));
			node.Add("m_Mesh2", Mesh2.ExportYAML(container));
			node.Add("m_Mesh3", Mesh3.ExportYAML(container));
			node.Add("m_MaskInteraction", (int)MaskInteraction);
			return node;
		}

		private float GetNormalDirection(Version version)
		{
			return IsReadNormalDirection(version) ? NormalDirection : 1.0f;
		}
		private IReadOnlyList<byte> GetVertexStreams(Version version)
		{
			return IsReadUseCustomVertexStreams(version) ? VertexStreams : new byte[] { 0, 1, 3, 4, 5 };
		}

		public ParticleSystemRenderMode RenderMode { get; private set; }
		public ParticleSystemSortMode SortMode { get; private set; }
		public float MinParticleSize { get; private set; }
		public float MaxParticleSize { get; private set; }
		public float CameraVelocityScale { get; private set; }
		public float VelocityScale { get; private set; }
		public float LengthScale { get; private set; }
		public float SortingFudge { get; private set; }
		public float NormalDirection { get; private set; }
		public ParticleSystemRenderSpace RenderAlignment { get; private set; }
		public bool UseCustomVertexStreams { get; private set; }
		public IReadOnlyList<byte> VertexStreams => m_vertexStreams;
		public SpriteMaskInteraction MaskInteraction { get; private set; }

		public Vector3f Pivot;
		public PPtr<Mesh> Mesh;
		public PPtr<Mesh> Mesh1;
		public PPtr<Mesh> Mesh2;
		public PPtr<Mesh> Mesh3;

		private byte[] m_vertexStreams;
	}
}
