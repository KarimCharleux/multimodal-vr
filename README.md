# 🎮 Projet VR : Espace Personnel PolyVision

## 📝 Description
Ce projet est conçu pour permettre aux étudiants de personnaliser leur avatar et leur espace virtuel. Le projet utilise Unity comme moteur de développement principal.

## 👨‍💻 Parties implémentées

### Saad BEIDOURI
* (*) Select an object in the space to visualize information about it
 - Système de sélection d'objets
 - Affichage des informations détaillées 
 - Interface utilisateur intuitive

* (**) Design a way for the user to change their appearance, by clicking on the different clothes and visual elements in their inventory
 - Système de personnalisation d'avatar
 - Interface de sélection des vêtements
 - Prévisualisation des changements en temps réel
   
### Anas CHHILIF
* (**) Design multimodal gestures that will allow the user to select and manipulate a decoration, including moving it around, scale and rotation
 - Système de manipulation d'objets
 - Gestes de redimensionnement
 - Contrôles de rotation

## ✅ Prérequis
Avant de commencer, assurez-vous d'avoir les éléments suivants installés :

* 🎯 **Unity Hub** avec Unity version 2021.3.3f1
* 📱 **Android Build Support** ou **iOS Build Support**
* 📦 **LeanTouch Package** pour la gestion des gestes
* 🎨 **SimplePeople Asset** pour les modèles d'avatars (LMS)

## ⚙️ Configuration du projet

### Configurer les paramètres de build 🛠️

1. Allez dans `File > Build Settings`
2. Sélectionnez Android comme plateforme
3. Cliquez sur `Switch Platform` pour appliquer les changements

### Configurer les paramètres VR 🎮

1. Allez dans `Edit > Project Settings > XR Plug-in Management`
2. Cochez Oculus sous `Plug-in Providers`

## 🚀 Exécution du projet

### Ouvrir la scène principale 🎬

1. Dans le dossier `Assets/Scenes`, ouvrez la scène `School`

### Build et déploiement 🏗️

1. Dans `File > Build Settings`, assurez-vous que la scène `School` et `SimpleScene` sont ajoutées à la liste des scènes à build
2. Cliquez sur `Build And Run`
