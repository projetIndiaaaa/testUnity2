using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chemin : NetworkBehaviour
{
    [SerializeField] private Transform groundCheck = null;
    [SerializeField] private Transform capsule = null;
    [SerializeField] private LayerMask checkMask;

    public Camera playerCam; 

    int InfiniteTime = 1000000;

    public int[,] path; //tableau dans lequel on range le chemin à parcourir

    private Vector3 positionCapsule = new Vector3(0, 0, 0);
    Vector3 target;
    bool checkDeplacement;
    bool checkDeplacementIntermediaire;
    bool enDeplacement;
    int deplacementIntermediaire;
    float limiteDistance = 0.00000005f;

    int deplacementX;
    int deplacementZ;

    public int ligne = 5;
    public int colonne = 10;
    public int[,] mapGrid;

    bool trig;
    int compteur;
    int i;
    bool enTrainDeCreerLaGrid;
    int nbRoute;
    int[,] tabRoute;
    int[,] tabVoisin;
    int num; //noramlement c'est la première ligne de sort
    int[] chemin;
    

    // Start is called before the first frame update
    void Start()
    {
        if (!isLocalPlayer)
        {
            playerCam.enabled = false;
        }

        
            positionCapsule = capsule.position;
            //capsule.Translate(Vector3.forward);

            //pas encore implémenté
            //path = cheminPossible(positionCapsule);

            groundCheck.position = new Vector3(0, 0, 0);
            mapGrid = new int[colonne, ligne];
            i = 0;
            nbRoute = 0;
            enTrainDeCreerLaGrid = true;
            //creationGrid(mapGrid);
            //groundCheck.position = new Vector3(2, 0, 2);
            creationGrid(mapGrid, trig);
            target = new Vector3(0, positionCapsule.y, 0);
        

    }

    // Update is called once per frame
    void Update()
    {
        

        if (isLocalPlayer)
        {
            RaycastHit hitInfo;
            //trouve la position sur laquelle le joueur à cliqué
            if (!checkDeplacement)
            {
                ///!\ PROBLEME ICI /!\
                //le 100 est pour la distance sur laquelle il cherche checkMask, le problème c'est qu'en regardant de coté, même en cliquant sur une case field il voit la route...
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 100, checkMask) && Input.GetMouseButtonUp(0))
                {

                    float xPoint = Mathf.Round(hitInfo.point.x);
                    float zPoint = Mathf.Round(hitInfo.point.z);
                    if ((xPoint != positionCapsule.x || zPoint != positionCapsule.z))
                    {
                        target = new Vector3(xPoint, positionCapsule.y, zPoint);
                        checkDeplacement = true;
                        checkDeplacementIntermediaire = true;
                    }


                }
            }
            //print(target);

            
        }
        deplacerCapsule();

    }

    /*
    private void FixedUpdate()
    {
        if (Physics.OverlapSphere(groundCheck.position, 0.1f, checkMask).Length == 0)
        {
            return;
        }
    }*/

    void deplacerCapsule()
    {
            if (capsule.position != target)
            {
                if (mapGrid[(int)target.x, (int)target.z] == 0)
                {//si target n'est pas sur une route on ne se déplace pas
                    target = positionCapsule;
                    checkDeplacement = false;
                    //print("yo");
                    return;
                }
                int[] TA = new int[nbRoute];
                int iCube = (int)target.x * ligne + (int)target.z;
                int iCapsule = (int)positionCapsule.x * ligne + (int)positionCapsule.z;
                //print("caspule " + iCapsule);


                if (checkDeplacementIntermediaire)
                {
                    calculeDijkstra(TA, iCube, iCapsule);
                    ShortestPath(iCapsule, TA, iCube);
                    checkDeplacementIntermediaire = false;
                    deplacementIntermediaire = (chemin.Length - 2);//Parce que la dernière case de chemin c'est la position de la capsule donc -2
                                                                   //print("déplacement intermediaire " + deplacementIntermediaire);
                }



                if (!enDeplacement && isLocalPlayer)
                {

                    deplacementX = chemin[deplacementIntermediaire] / ligne; //remplace le target.x
                    deplacementZ = chemin[deplacementIntermediaire] % ligne; //remplace le target.z
                                                                             //print("x " + deplacementX + " z " + deplacementZ);
                    /*norme = Mathf.Sqrt(Mathf.Pow(deplacementX - positionCapsule.x, 2) + Mathf.Pow(deplacementZ - positionCapsule.z, 2));
                    if (norme == 0)
                    {
                        norme = 1;
                    }*/
                    //print("vector3 : " + ((deplacementX - positionCapsule.x) / norme) + ", " + 0 + ", " + ((deplacementZ - positionCapsule.z) / norme));

                    enDeplacement = true;

                }

                if (enDeplacement)
                {
                    capsule.Translate((new Vector3(((deplacementX - positionCapsule.x) /*/ norme*/), 0, (deplacementZ - positionCapsule.z) /*/ norme*/)) * Time.deltaTime * 2);
                    //Je crois bien que les deux premières lignes ne servent à rien
                    if (((capsule.position.x >= deplacementX && capsule.position.x <= deplacementX + limiteDistance) || (capsule.position.x <= deplacementX && capsule.position.x >= deplacementX - limiteDistance)) &&
                        ((capsule.position.z >= deplacementZ && capsule.position.z <= deplacementZ + limiteDistance) || (capsule.position.z <= deplacementZ && capsule.position.z >= deplacementZ - limiteDistance)) ||
                          (positionCapsule.x < deplacementX && capsule.position.x > deplacementX) || (positionCapsule.x > deplacementX && capsule.position.x < deplacementX) ||
                          (positionCapsule.z < deplacementZ && capsule.position.z > deplacementZ) || (positionCapsule.z > deplacementZ && capsule.position.z < deplacementZ))
                    {
                        //print("yo");
                        //print(target);
                        capsule.position = new Vector3(deplacementX, positionCapsule.y, deplacementZ);
                        //capsule.Translate(new Vector3(target.x - positionCapsule.x, 0, target.z - positionCapsule.z));
                        positionCapsule = capsule.position;
                        enDeplacement = false;
                        if (deplacementIntermediaire == 0)
                        {
                            checkDeplacement = false;
                            return;
                        }
                        deplacementIntermediaire--;

                    }
                }
        }
    }

    void ShortestPath(int iCapsule, int[] TA, int iCube) //iCube est le cube sur lequelle on a cliqué et iCapsule est la position de la capsule 
    {
        int nbCubeChemin = 0;
        int posCubeActuel = iCube;
        for (int i = 0; i < nbRoute; i++)
        {
            nbCubeChemin++;
            if (posCubeActuel == iCapsule) break;
            int j;
            for(j=0; j < nbRoute; j++)
            {
                if (tabRoute[j, 0] == posCubeActuel)
                {
                    posCubeActuel = tabRoute[tabRoute[j, 2], 0];
                    break;
                }
            }
        }

        posCubeActuel = iCube;
        chemin = new int[nbCubeChemin];
        for(int i = 0; i < nbRoute; i++)
        {
            chemin[i] = posCubeActuel;
            if (posCubeActuel == iCapsule) break;
            int j;
            for (j = 0; j < nbRoute; j++)
            {
                if (tabRoute[j, 0] == posCubeActuel)
                {
                    posCubeActuel = tabRoute[tabRoute[j, 2], 0];
                    break;
                }
            } 
        }

        for(int i=0; i<nbCubeChemin; i++)
        {
            //print("Chemin " + chemin[i]);
        }

        /*Noeud* noeudCourant = listeNoeud[this->parent];
        Noeud* noeudPasse = this;
        for (unsigned int i(0); i < listeNoeud.size(); i++)
        {
            for (unsigned int j(0); j < listeLiens.size(); j++)
            {
                if ((listeLiens[j].noeud1->uid == noeudCourant->uid &&
                    listeLiens[j].noeud2->uid == noeudPasse->uid) ||
                   (listeLiens[j].noeud2->uid == noeudCourant->uid &&
                    listeLiens[j].noeud1->uid == noeudPasse->uid))
                {
                    listeLiens[j].couleur = VERT;
                    break;
                }
            }//Fin for j
            if (noeudCourant->uid == noeud->uid) break;
            //setCouleur(VERT, noeudCourant, listeNoeud);
            noeudPasse = noeudCourant;
            noeudCourant = listeNoeud[noeudCourant->parent];

        }//Fin for i*/
    }

    private void FixedUpdate() {

    }
    void creationGrid(int[,] mapGrid, bool trig)
    {
        //Je le met ici au cas où OnTriggerEnter est appelé plus tard dans la game
        //parce que si on s'arrête à ligne*colonne on rate le dernier
        if (i == (ligne * colonne))
        {
            enTrainDeCreerLaGrid = false;
            //print(mapGrid);
            return;
        }
        //print(groundCheck.position.x + ", " + groundCheck.position.y + ", " + groundCheck.position.z);
        //int longueur = Physics.OverlapSphere(groundCheck.position, 0.1f).Length;
        //print(longueur);
        for (int i = 0; i < ligne * colonne; i++)
        {
            groundCheck.position = new Vector3((int)i / (int)ligne, 0, i % ligne);
            if (Physics.OverlapSphere(groundCheck.position, 0.1f).Length == 0) {
                mapGrid[(int)i / (int)ligne, i % ligne] = 0;
                //print(0);
            } else if (Physics.OverlapSphere(groundCheck.position, 0.1f)[0].gameObject.layer == 9)
            {
                mapGrid[(int)i / (int)ligne, i % ligne] = 1;
                nbRoute++;

                //print(1);
            }
            else if (Physics.OverlapSphere(groundCheck.position, 0.1f)[0].gameObject.layer == 10)
            {
                mapGrid[(int)i / (int)ligne, i % ligne] = 0;
                //print(0);
            }
            //print(Physics.OverlapSphere(groundCheck.position, 0.1f)[i]);

        }
        tabRoute = new int[nbRoute, 4];
        tabVoisin = new int[nbRoute, 4];
        tabVoisinAMoinsUn();
        int j = 0;
        for (int i = 0; i < ligne * colonne; i++)
        {
            if (mapGrid[(int)i / (int)ligne, i % ligne] == 1)
            {
                tabRoute[j, 0] = i;
                j++;
            }
        }

        //maintenant on va créer tabVoisin, on regarde tout autour
        //j = 0;
        for (int i = 0; i < ligne * colonne; i++)
        {
            if (mapGrid[(int)i / (int)ligne, i % ligne] == 1)
            {
                int l;
                for(l=0; l < nbRoute; l++)
                {
                    if (tabRoute[l, 0] == (i))
                    {
                        
                        break;
                    }
                    
                }
                if (!((i % ligne) + 1 > (ligne -1)))
                {
                    if (mapGrid[((int)i / (int)ligne), (i % ligne) + 1] == 1)
                    {
                        for (int k = 0; k < nbRoute; k++)
                        {
                            if (tabRoute[k, 0] == (i + 1))
                            {
                                tabVoisin[l, 0] = k;
                                break;
                            }
                        }
                    }
                    //print("test "+ (((int)i / (int)ligne) + 1));
                    //print(!((((int)i / (int)ligne) + 1) > (colonne - 1)));
                }if(!((((int)i / (int)ligne) + 1) > (colonne-1)))
                {
                    //print("yo");
                    if (mapGrid[((int)i / (int)ligne) + 1, (i % ligne)] == 1)
                    {
                        for (int k = 0; k < nbRoute; k++)
                        {
                            if (tabRoute[k, 0] == (i + ligne))
                            {
                                tabVoisin[l, 1] = k;
                                break;
                            }
                        }
                    }
                }
                if (!((i % ligne) - 1 < 0))
                {
                    if (mapGrid[((int)i / (int)ligne), (i % ligne) - 1] == 1)
                    {
                        for (int k = 0; k < nbRoute; k++)
                        {
                            if (tabRoute[k, 0] == (i - 1))
                            {
                                tabVoisin[l, 2] = k;
                                break;
                            }
                        }
                    }
                }
                if (!((((int)i / (int)ligne) -1) < 0))
                {
                    if (mapGrid[((int)i / (int)ligne) - 1, (i % ligne)] == 1)
                    {
                        for (int k = 0; k < nbRoute; k++)
                        {
                            if (tabRoute[k, 0] == (i - ligne))
                            {
                                tabVoisin[l, 3] = k;
                                break;
                            }
                        }
                    }
                }
                //j++;
            }//Fin if mapGrid == 1
        }//Fin for i

        /*for(int i = 0; i < nbRoute; i++)
        {
            print("tabVoisin ligne " + i + ": haut " + tabVoisin[i, 0] + " droite " + tabVoisin[i, 1] + " bas " + tabVoisin[i, 2] + " gauche " + tabVoisin[i, 3]);
        }*/
        
        //print("creationGrid");
    }//Fin creationGrid

    void tabVoisinAMoinsUn()
    {
        for(int i = 0; i < nbRoute; i++)
        {
            for(int j=0; j < 4; j++)
            {
                tabVoisin[i, j] = -1;
            }
        }
    }

    void calculeDijkstra(int[] TA, int iCube, int iCapsule)//TA a la longueur nbRoute
    {
        //int size = tabRoute.Length; //size nbRoute
        for (int i = 0; i < nbRoute; i++)
        {
            tabRoute[i, 1] = 1;
            tabRoute[i, 2] = -1;
            if (tabRoute[i, 0] == iCapsule) tabRoute[i, 3] = 0;
            else tabRoute[i, 3] = InfiniteTime;
        }

        for (int i = 0; i < nbRoute; i++)
        {
            if (tabRoute[i, 0] == iCapsule)
            {
                TA[0] = i;
                TA[i] = 0;
            }
            else TA[i] = i;
        }

        for (int i = 0; i < nbRoute; i++)
        {//size égale listeNoeud.size()
            int posmin = find_min_access();
            //Noeud* n = listeNoeud[posmin];
            tabRoute[posmin, 1] = 0;//n->in = false;
            //if (n->type == PRODUCTION) continue;//On peut pas traverser un noeud production
            for (int j=0; j < 4; j++) { //n->noeudVoisin.size()
                if (tabVoisin[posmin, j] == -1) continue; //on est hors de la carte
                if(tabRoute[tabVoisin[posmin, j],1] == 1) // tabRoute[tabVoisin[posmin, j],*] == n->noeudVoisin[j]
                {
                    int alt = (tabRoute[posmin, 3] + 1);//il faut un temps 1 pour aller d'un cube à l'autre, du coup on s'en fou de compute access
                    if(tabRoute[tabVoisin[posmin, j], 3] > alt)
                    {
                        tabRoute[tabVoisin[posmin, j], 3] = alt;
                        tabRoute[tabVoisin[posmin, j], 2] = posmin;
                        sort(TA, posmin, j);
                    }
                }
                /*if (n->noeudVoisin[j]->in){ 
                    double alt = n->access + compute_access(n, n->noeudVoisin[j]);
                    if (n->noeudVoisin[j]->access > alt)
                    {
                        n->noeudVoisin[j]->access = alt;
                        n->noeudVoisin[j]->parent = posmin;
                        sort(TA, n->noeudVoisin[j], listeNoeud);
                    }
                }*/
            
             }//Fin for avec j
         }//Fin for avec i
    }

    void sort(int[] TA, int posmin, int j) //tabRoute[tabVoisin[posmin, j],*] == voisin
    {
        
        for (int i=0; i < nbRoute; i++)
        {
            if (tabRoute[i, 0] == tabRoute[tabVoisin[posmin, j], 0])
            {
                num = i;
                break;
            }
        }
        for (i=0; i < nbRoute; i++)
        {
            if (tabRoute[tabVoisin[posmin, j], 3] <= tabRoute[TA[i], 3])
            {
                int temp2 = num;
                for (int k=i; k < nbRoute; k++)
                {

                    int temp = TA[k];
                    TA[k] = temp2;
                    temp2 = temp;
                    if (temp == num) return;
                }//Fin for avec k
                break;
            }
        }//Fin gros for (avec i)
    }//Fin sort

    int find_min_access()
    {
        int min = InfiniteTime;
        int posmin = 0;//-1; //test pas concluant avec -1
        for (int i=0; i < nbRoute; i++)
        {
            if (tabRoute[i,1] == 1 && min > tabRoute[i,3]){
            min = tabRoute[i,3];
            posmin = i;
            }
        }
        //print("posmin " + posmin);
        return posmin;
    }

    


    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            /*trig = true;
            creationGrid(mapGrid, trig);
            print("onTriggerStay9");

            //Destroy(other.gameObject);*/
        }else if (other.gameObject.layer == 10)
        {
            /*creationGrid(mapGrid, trig);
            print("onTriggerStay10");*/
        }
    }
    /*private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            trig = true;
            creationGrid(mapGrid, trig);
            print("onTriggerEnter");

            //Destroy(other.gameObject);
        }

    }*/
    /*
    void plusCourtChemin()
    {
        
        groundCheck;
    }

    int[,] cheminPossible(Vector3 positionCapsule)
    {
        int[,] chemin = new int[2, 10];

        return chemin;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            //Destroy(other.gameObject);
            //superJumpsRemaining++;
        }

    }
    bool TestDirection(int x, int y, int direction)
    {
        //int direction tells which case to use, 1 is up, 2 is right, 3 is down, 4 is left
        switch (direction)
        {
            case 1:
                if (groundCheck.position.x)
                    return true;
                else
                    return false;
            case 2:
                if (x + 1 < columns && gridArray[x + 1, y] && gridArray[x + 1, y].GetComponent<GridState>().visited == step)
                    return true;
                else
                    return false;
            case 3:
                if (y - 1 < -1 && gridArray[x, y - 1] && gridArray[x, y - 1].GetComponent<GridState>().visited == step)
                    return true;
                else
                    return false;
            case 4:
                if (x - 1 < -1 && gridArray[x - 1, y] && gridArray[x - 1, y].GetComponent<GridState>().visited == step)
                    return true;
                else
                    return false;
        }
        return false;
    }
    //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /*
    void calculeDijkstra(vector<Noeud*>& listeNoeud, int[]& TA)
    {
        unsigned int size = listeNoeud.size();
        for (unsigned int i(0); i < size; i++)
        {
            listeNoeud[i]->in = true;
            listeNoeud[i]->parent = no_link;
            if (listeNoeud[i]->uid == this->uid) listeNoeud[i]->access = 0;
            else listeNoeud[i]->access = infinite_time;
        }

        for (unsigned int i(0); i < size; i++)
        {
            if (listeNoeud[i]->uid == this->uid)
            {
                TA[0] = i;
                TA[i] = 0;
            }
            else TA[i] = i;
        }

        for (unsigned int i(0); i < size; i++)
        {//size égale listeNoeud.size()
            int posmin = find_min_access(size, listeNoeud);
            /*if(posmin == -1){
                for(unsigned int j(0); j<TA.size(); j++){
                    if(listeNoeud[TA[i]]->in){
                        posmin = TA[i];
                        break;
                    }
                }}
            Noeud* n = listeNoeud[posmin];
            n->in = false;
            if (n->type == PRODUCTION) continue;//On peut pas traverser un noeud production
            for (unsigned int j(0); j < n->noeudVoisin.size(); j++)
            {
                if (n->noeudVoisin[j]->in){
            double alt = n->access + compute_access(n, n->noeudVoisin[j]);
            if (n->noeudVoisin[j]->access > alt)
            {
                n->noeudVoisin[j]->access = alt;
                n->noeudVoisin[j]->parent = posmin;
                sort(TA, n->noeudVoisin[j], listeNoeud);
            }
            }
        }
    }//Fin for avec j
    }//Fin for avec i
    }

    void Noeud::sort(vector<int>& TA, Noeud* voisin, vector<Noeud*> listeNoeud)
    {
        int num;
        for (unsigned int i(0); i < listeNoeud.size(); i++)
        {
            if (listeNoeud[i]->uid == voisin->uid)
            {
                num = i;
                break;
            }
        }
        for (unsigned int i(0); i < TA.size(); i++)
        {
            if (voisin->access <= listeNoeud[TA[i]]->access)
            {
                int temp2 = num;
                for (unsigned int j(i); j < TA.size(); j++)
                {

                    int temp = TA[j];
                    TA[j] = temp2;
                    temp2 = temp;
                    if (temp == num) return;
                }//Fin for avec j
                break;
            }
        }//Fin gros for (avec i)
    }//Fin sort

    double compute_access(Noeud* n, Noeud* voisin)
    {
        double dist = sqrt(pow(n->taille.centre.x - voisin->taille.centre.x, 2) +
                             pow(n->taille.centre.y - voisin->taille.centre.y, 2));
        double taccess = 0;
        if (n->type == TRANSPORT && voisin->type == TRANSPORT)
        {
            taccess = dist / fast_speed;
        }
        else
        {
            taccess = dist / default_speed;
        }
        return taccess;
    }

    int find_min_access(unsigned int size, vector<Noeud*> listeNoeud)
    {
        double min = infinite_time;
        int posmin = 0;//-1; //test pas concluant avec -1
        for (unsigned int i(0); i < size; i++)
        {
            if (listeNoeud[i]->in && min > listeNoeud[i]->access){
            min = listeNoeud[i]->access;
            posmin = i;
        }
    }

    return posmin;
    }

    }*/
}
