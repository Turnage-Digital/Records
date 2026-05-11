import shopifyEslintPlugin from "@shopify/eslint-plugin";

const buildRestrictedImportPaths = (path) => [
    `./${path}`,
    `../${path}`,
    `../../${path}`,
    `./${path}/index`,
    `../${path}/index`,
    `../../${path}/index`,
];

const restrictedBarrelImports = [
    ...buildRestrictedImportPaths("components"),
    ...buildRestrictedImportPaths("models"),
    ...buildRestrictedImportPaths("pages"),
    ...buildRestrictedImportPaths("history"),
    ...buildRestrictedImportPaths("notifications"),
    ...buildRestrictedImportPaths("record-editor"),
    ...buildRestrictedImportPaths("records-display"),
    ...buildRestrictedImportPaths("recordset-editor"),
    ...buildRestrictedImportPaths("side-drawer"),
];

// eslint-disable-next-line import/no-anonymous-default-export
export default [
    ...shopifyEslintPlugin.configs.typescript,
    ...shopifyEslintPlugin.configs["typescript-type-checking"],
    {
        languageOptions: {
            parserOptions: {
                project: "tsconfig.json",
            },
        },
    },
    ...shopifyEslintPlugin.configs.react,
    ...shopifyEslintPlugin.configs.prettier,
    {
        rules: {
            "@shopify/strict-component-boundaries": "off",
            "@shopify/jsx-no-hardcoded-content": "off",
            "@typescript-eslint/naming-convention": "off",
            "@typescript-eslint/no-misused-promises": "off",
            "id-length": "off",
            "no-process-env": "off",
            "no-implicit-coercion": "off",
            "no-template-curly-in-string": "off",
            "no-restricted-imports": [
                "error",
                {
                    paths: restrictedBarrelImports.map((name) => ({
                        name,
                        message:
                            "Import from the exact source file instead of a barrel directory.",
                    })),
                },
            ],
            "import/order": [
                "error",
                {
                    groups: [
                        "builtin",
                        "external",
                        "internal",
                        ["sibling", "parent"],
                        "index",
                        "object",
                        "type",
                    ],
                    pathGroups: [
                        {
                            pattern: "react",
                            group: "external",
                            position: "before",
                        },
                    ],
                    pathGroupsExcludedImportTypes: ["react"],
                    alphabetize: {
                        order: "asc",
                        caseInsensitive: true,
                    },
                    "newlines-between": "always",
                },
            ],
        },
    },
];
