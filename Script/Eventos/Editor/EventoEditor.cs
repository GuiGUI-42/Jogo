using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Evento))]
public class EventoEditor : Editor
{
    SerializedProperty nomeEventoProp;
    SerializedProperty iconeEventoProp;
    SerializedProperty descricaoEventoProp;
    SerializedProperty localProp;
    SerializedProperty slotsNecessariosProp;
    
    // --- NOVAS PROPRIEDADES ---
    SerializedProperty categoriaProp;
    SerializedProperty antecessorProp;
    // --------------------------

    SerializedProperty semanaMinProp;
    SerializedProperty semanaMaxProp;
    SerializedProperty recompensaReputacaoProp;
    SerializedProperty recompensaOuroProp;
    SerializedProperty monstroPrefabProp;
    SerializedProperty opcoesDecisaoProp;

    void OnEnable()
    {
        nomeEventoProp = serializedObject.FindProperty("nomeEvento");
        iconeEventoProp = serializedObject.FindProperty("iconeEvento");
        descricaoEventoProp = serializedObject.FindProperty("descricaoEvento");
        localProp = serializedObject.FindProperty("local");
        slotsNecessariosProp = serializedObject.FindProperty("slotsNecessarios");
        
        // --- LOCALIZANDO ---
        categoriaProp = serializedObject.FindProperty("categoria");
        antecessorProp = serializedObject.FindProperty("antecessor");
        // -------------------

        semanaMinProp = serializedObject.FindProperty("semanaMin");
        semanaMaxProp = serializedObject.FindProperty("semanaMax");
        recompensaReputacaoProp = serializedObject.FindProperty("recompensaReputacao");
        recompensaOuroProp = serializedObject.FindProperty("recompensaOuro");
        monstroPrefabProp = serializedObject.FindProperty("monstroPrefab");
        opcoesDecisaoProp = serializedObject.FindProperty("opcoesDecisao");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Informações Básicas", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nomeEventoProp);
        EditorGUILayout.PropertyField(iconeEventoProp);
        EditorGUILayout.PropertyField(descricaoEventoProp);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configurações de Spawn", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(localProp);
        EditorGUILayout.PropertyField(slotsNecessariosProp);

        // --- LÓGICA DE EXIBIÇÃO DA AVENTURA ---
        EditorGUILayout.PropertyField(categoriaProp);
        
        // Se for Aventura, mostra o campo para linkar o antecessor
        CategoriaEvento cat = (CategoriaEvento)categoriaProp.enumValueIndex;
        if (cat == CategoriaEvento.Aventura)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(antecessorProp, new GUIContent("Evento Antecessor", "O evento que precisa ser concluído antes deste. Deixe vazio se for o primeiro da aventura."));
            EditorGUI.indentLevel--;
        }
        // ---------------------------------------

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Intervalo de Semanas", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(semanaMinProp, new GUIContent("Semana Mín"));
        EditorGUILayout.PropertyField(semanaMaxProp, new GUIContent("Semana Máx"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recompensas", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(recompensaReputacaoProp, new GUIContent("Reputação"));
        EditorGUILayout.PropertyField(recompensaOuroProp, new GUIContent("Ouro"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inimigo (Se houver combate)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(monstroPrefabProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Opções de Decisão", EditorStyles.boldLabel);
        DrawOpcoes(opcoesDecisaoProp);

        serializedObject.ApplyModifiedProperties();
    }

    // (Mantenha o método DrawOpcoes inalterado...)
    void DrawOpcoes(SerializedProperty arrayProp) 
    {
        // ... Copie o conteúdo existente do DrawOpcoes que já estava no arquivo ...
        // Para economizar espaço na resposta, assumo que você manterá o método original
        // que desenha a lista de opções, passivos e drops.
        if (arrayProp == null) return;
        EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var elem = arrayProp.GetArrayElementAtIndex(i);
            // ... (todo o código de desenho das opções)
             var nomeProp = elem.FindPropertyRelative("nomeOpcao");
            var descProp = elem.FindPropertyRelative("descricao");
            var iconeProp = elem.FindPropertyRelative("icone");
            var usarIconeProp = elem.FindPropertyRelative("usarIconeDaOpcao");
            var tipoProp = elem.FindPropertyRelative("tipo");
            var efeitosProp = elem.FindPropertyRelative("efeitosPassivos");
            var dropsProp = elem.FindPropertyRelative("drops");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Opção {i+1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remover", GUILayout.Width(70)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(nomeProp);
            EditorGUILayout.BeginHorizontal();
            usarIconeProp.boolValue = EditorGUILayout.ToggleLeft("Usar Ícone da Opção", usarIconeProp.boolValue, GUILayout.Width(160));
            if (usarIconeProp.boolValue)
            {
                EditorGUILayout.PropertyField(iconeProp, GUIContent.none);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(descProp);
            EditorGUILayout.PropertyField(tipoProp);

            var tipo = (TipoEvento)tipoProp.enumValueIndex;
            if (tipo == TipoEvento.Passivo)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Modificadores Passivos", EditorStyles.boldLabel);
                for (int m = 0; m < efeitosProp.arraySize; m++)
                {
                    var mod = efeitosProp.GetArrayElementAtIndex(m);
                    var atributoProp = mod.FindPropertyRelative("atributo");
                    var valorProp = mod.FindPropertyRelative("valor");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(atributoProp, GUIContent.none);
                    valorProp.intValue = EditorGUILayout.IntField(valorProp.intValue, GUILayout.Width(60));
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        efeitosProp.DeleteArrayElementAtIndex(m);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Adicionar Modificador"))
                {
                    efeitosProp.arraySize++;
                    var novo = efeitosProp.GetArrayElementAtIndex(efeitosProp.arraySize - 1);
                    novo.FindPropertyRelative("atributo").enumValueIndex = 0;
                    novo.FindPropertyRelative("valor").intValue = 0;
                }
                EditorGUILayout.EndVertical();
            }

            // Drops
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Drops", EditorStyles.boldLabel);
            for (int d = 0; d < dropsProp.arraySize; d++)
            {
                var drop = dropsProp.GetArrayElementAtIndex(d);
                var itemProp = drop.FindPropertyRelative("item");
                var qMinProp = drop.FindPropertyRelative("quantidadeMin");
                var qMaxProp = drop.FindPropertyRelative("quantidadeMax");
                var chanceProp = drop.FindPropertyRelative("chance");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(itemProp, GUIContent.none, GUILayout.Width(180));
                qMinProp.intValue = EditorGUILayout.IntField(qMinProp.intValue, GUILayout.Width(40));
                qMaxProp.intValue = EditorGUILayout.IntField(qMaxProp.intValue, GUILayout.Width(40));
                chanceProp.floatValue = EditorGUILayout.Slider(chanceProp.floatValue, 0f, 1f);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    dropsProp.DeleteArrayElementAtIndex(d);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Adicionar Drop"))
            {
                dropsProp.arraySize++;
                var novo = dropsProp.GetArrayElementAtIndex(dropsProp.arraySize - 1);
                novo.FindPropertyRelative("item").objectReferenceValue = null;
                novo.FindPropertyRelative("quantidadeMin").intValue = 1;
                novo.FindPropertyRelative("quantidadeMax").intValue = 1;
                novo.FindPropertyRelative("chance").floatValue = 1f;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("Adicionar Opção"))
        {
            arrayProp.arraySize++;
        }
        EditorGUILayout.EndVertical();
    }
}