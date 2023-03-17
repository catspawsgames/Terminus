using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus.Demo2
{
	public class WorldBuilder : MonoBehaviour {

		public Transform followTarget;
		public float sectorSize = 60;

		public int minPartsPerSector;
		public int maxPartsPerSector;
		public int minShipsPerSector;
		public int maxShipsPerSector;

		public GameObject[] partsPrefabs;
		public SerializableAssembly[] shipAssemblies;

		protected GameObject[] assembledShipOriginals;

		public struct SectorCoords
		{
			public int x;
			public int y;

			public SectorCoords (int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

		public class SectorInfo
		{
			public float threatLevel;
		}


		protected SectorCoords currentSector = new SectorCoords(10000,10000);
		protected SectorCoords oldSector = new SectorCoords(10000,10000);

		protected SectorInfo[,] sectors = new SectorInfo[1024,1024];

		public int SectorDistance(SectorCoords sector1,SectorCoords sector2)
		{
			return Mathf.Max(Mathf.Abs((sector1.x-sector2.x)),Mathf.Abs((sector1.y-sector2.y)));
		}

		public SectorCoords GetSector(Vector3 position)
		{
			return new SectorCoords(Mathf.RoundToInt(position.x/sectorSize),Mathf.RoundToInt(position.y/sectorSize));
		}

		public SectorCoords SectorShift(SectorCoords original, int x, int y)
		{
			return new SectorCoords(original.x+x,original.y+y);
		}

		public void FillSector(SectorCoords sector)
		{
			int partsCount = UnityEngine.Random.Range(minPartsPerSector,maxPartsPerSector);
			for (int i = 0; i < partsCount; i++)
			{
				int partInd = UnityEngine.Random.Range(0,partsPrefabs.Length);
				GameObject part = (GameObject)Instantiate(partsPrefabs[partInd]);
				part.transform.position = new Vector3(sector.x * sectorSize + UnityEngine.Random.Range(-sectorSize/2,sectorSize/2),
				                                      sector.y * sectorSize + UnityEngine.Random.Range(-sectorSize/2,sectorSize/2),
				                                      0);
				part.transform.rotation = Quaternion.Euler(new Vector3(0,0,UnityEngine.Random.Range(0,360)));
				ShipAttachableBlock attBlock = part.GetComponent<ShipAttachableBlock>();
				attBlock.health = UnityEngine.Random.Range(attBlock.maxHealth/2,attBlock.maxHealth);
			}

			if (shipAssemblies.Length > 0)
			{
				int shipCount = UnityEngine.Random.Range(minShipsPerSector,maxShipsPerSector);
				for (int i = 0; i < shipCount; i++)
				{
					int shipInd = UnityEngine.Random.Range(0,shipAssemblies.Length);
					GameObject obj = (GameObject)Instantiate(assembledShipOriginals[shipInd]);
					obj.SetActive(true);
					ShipAIController controller = obj.GetComponent<ShipAIController>();
					controller.moveTarget = new Vector2(UnityEngine.Random.Range(-10000,10000),UnityEngine.Random.Range(-10000,10000));
					obj.transform.position = new Vector3(sector.x * sectorSize + UnityEngine.Random.Range(-sectorSize/2,sectorSize/2),
					                                     sector.y * sectorSize + UnityEngine.Random.Range(-sectorSize/2,sectorSize/2),
					                                     0);
					obj.transform.rotation = Quaternion.Euler(new Vector3(0,0,UnityEngine.Random.Range(0,360)));
					Color shipCol = new Color(0,UnityEngine.Random.value,UnityEngine.Random.value);
					float normalizeCoef = 1 / Mathf.Max(shipCol.g,shipCol.b);
					shipCol = shipCol * normalizeCoef;
					obj.GetComponent<Ship>().ChangeShipColor(shipCol);
				}
			}
		}

		void Update()
		{
			if (followTarget != null)
				transform.position = followTarget.transform.position;
			
			currentSector = GetSector(transform.position);			

			if (!currentSector.Equals(oldSector))
			{
				List<TerminusObject> rootObjects = Manager.Instance.rootObjects;
				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						SectorCoords checkOldSector = SectorShift(oldSector,x,y);
						SectorCoords checkNewSector = SectorShift(currentSector,x,y);

						//Destroying all parts that are outside active 3x3 sector grid
						if (SectorDistance(checkOldSector,currentSector) >= 2)
                        {                            	                        
							for (int i = 0; i < rootObjects.Count; i++)
							{
								if (GetSector(rootObjects[i].transform.position).Equals(checkOldSector))
								{
									Destroy(rootObjects[i].gameObject);
								}
							}
						}

						//Filling previously unfilled sectors inside active 3x3 sector grid
						if (SectorDistance(checkNewSector,oldSector) >= 2)
						{
							FillSector(checkNewSector);
						}
					}
				}
				oldSector = currentSector;
			}

		}

		void Awake()
		{
			assembledShipOriginals = new GameObject[shipAssemblies.Length];
			for (int i = 0; i < shipAssemblies.Length; i++)
			{
				TerminusObject obj = shipAssemblies[i].Spawn(Vector3.zero,TerminusObject.Modes.inactive);
				assembledShipOriginals[i] = obj.gameObject;
				obj.gameObject.AddComponent<ShipAIController>();
				obj.gameObject.SetActive(false);
			}
		}
	}
}