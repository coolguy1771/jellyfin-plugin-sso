const ssoConfigurationPage = {
  pluginUniqueId: "505ce9d1-d916-42fa-86ca-673ef241d7df",
  loadConfiguration: (page) => {
    ApiClient.getPluginConfiguration(ssoConfigurationPage.pluginUniqueId).then(
      (config) => {
        ssoConfigurationPage.populateProviders(page, config.OidConfigs);
        ssoConfigurationPage.populateSamlProviders(
          page,
          config.SamlConfigs || {},
        );
      },
    );

    const folder_container = page.querySelector("#EnabledFolders");
    ssoConfigurationPage.populateFolders(folder_container);
    const saml_folder_container = page.querySelector("#SamlEnabledFolders");
    if (saml_folder_container)
      ssoConfigurationPage.populateFolders(saml_folder_container);
  },
  populateSamlProviders: (page, samlConfigs) => {
    const sel = page.querySelector("#selectSamlProvider");
    if (!sel) return;
    sel.querySelectorAll("option").forEach((opt) => {
      opt.remove();
    });
    Object.keys(samlConfigs).forEach((name) => {
      sel.appendChild(new Option(name, name));
    });
  },
  populateProviders: (page, providers) => {
    // Clear providers in case there are out of date ones
    page
      .querySelector("#selectProvider")
      .querySelectorAll("option")
      .forEach((option) => {
        option.remove();
      });

    // Add providers as options for the selector

    Object.keys(providers).forEach((provider_name) => {
      var choice = new Option(provider_name, provider_name);

      page.querySelector("#selectProvider").appendChild(choice);
    });
  },
  populateEnabledFolders: (folder_list, container) => {
    container.querySelectorAll(".folder-checkbox").forEach((e) => {
      e.checked = folder_list.includes(e.getAttribute("data-id"));
    });
  },
  serializeEnabledFolders: (container) => {
    return [...container.querySelectorAll(".folder-checkbox")]
      .filter((e) => e.checked)
      .map((e) => {
        return e.getAttribute("data-id");
      });
  },
  populateFolders: (container) => {
    return ApiClient.getJSON(
      ApiClient.getUrl("Library/MediaFolders", {
        IsHidden: false,
      }),
    ).then((folders) => {
      ssoConfigurationPage._populateFolders(container, folders);
    });
  },
  /*
  container: html element
  folders.Items: array of objects, with .Id & .Name
  */
  _populateFolders: (container, folders) => {
    container
      .querySelectorAll(".emby-checkbox-label")
      .forEach((e) => e.remove());

    const checkboxes = folders.Items.map((folder) => {
      var out = document.createElement("label");
      out.className = "emby-checkbox-label";
      var input = document.createElement("input");
      input.setAttribute("is", "emby-checkbox");
      input.className = "folder-checkbox chkFolder";
      input.setAttribute("data-id", String(folder.Id));
      input.type = "checkbox";
      var span = document.createElement("span");
      span.textContent = folder.Name != null ? folder.Name : "";
      out.appendChild(input);
      out.appendChild(span);
      return out;
    });

    checkboxes.forEach((e) => {
      container.appendChild(e);
    });
  },

  populateRoleMappings: (folder_role_mappings, container) => {
    container
      .querySelectorAll(".sso-role-mapping-container")
      .forEach((e) => e.remove());

    const mapping_elements = folder_role_mappings.map((mapping) => {
      var elem = document.createElement("div");

      elem.classList.add("sso-role-mapping-container");
      elem.innerHTML = `
      <label
        class="inputLabel inputLabelUnfocused sso-role-mapping-input-label" 
      >Role:</label>
      <div class="listItem">
        <input
          is="emby-input"
          required=""
          type="text"
          class="listItemBody sso-role-mapping-name"
        />
        <button
          type="button"
          is="paper-icon-button-light"
          class="listItemButton sso-remove-role-mapping"
        >
          <span class="material-icons remove_circle" aria-hidden="true"></span>
        </button> 
      </div> 
      <div
        class="checkboxList paperList sso-folder-list"
      ></div>
      `;

      var checklist = elem.querySelector(".sso-folder-list");
      const enabled_folders = mapping["Folders"];

      ssoConfigurationPage
        .populateFolders(checklist)
        .then(() =>
          ssoConfigurationPage.populateEnabledFolders(
            enabled_folders,
            checklist,
          ),
        );

      elem.querySelector(".sso-role-mapping-name").value = mapping["Role"];
      elem
        .querySelector(".sso-remove-role-mapping")
        .addEventListener(
          "click",
          ssoConfigurationPage.handleRoleMappingRemove,
        );

      return elem;
    });

    mapping_elements.forEach((e) => container.appendChild(e));
  },
  serializeRoleMappings: (container) => {
    const out = [];
    container
      .querySelectorAll(".sso-role-mapping-container")
      .forEach((elem) => {
        const role = elem.querySelector(".sso-role-mapping-name").value;
        const checklist = elem.querySelector(".sso-folder-list");
        out.push({
          Role: role,
          Folders: ssoConfigurationPage.serializeEnabledFolders(checklist),
        });
      });
    return out;
  },
  handleRoleMappingRemove: (evt) => {
    const targeted_mapping = evt.target.closest(".sso-role-mapping-container");
    targeted_mapping.remove();
  },
  listArgumentsByType: (page) => {
    const json_class = ".sso-json";
    const toggle_class = ".sso-toggle";
    const text_class = ".sso-text";
    const text_list_class = ".sso-line-list";

    const folder_list_fields = ["EnabledFolders"];
    const role_map_fields = ["FolderRoleMapping"];

    const oidc_form = page.querySelector("#sso-new-oidc-provider");

    const text_fields = [...oidc_form.querySelectorAll(text_class)].map(
      (e) => e.id,
    );

    const json_fields = [...oidc_form.querySelectorAll(json_class)].map(
      (e) => e.id,
    );

    const text_list_fields = [
      ...oidc_form.querySelectorAll(text_list_class),
    ].map((e) => e.id);

    const check_fields = [...oidc_form.querySelectorAll(toggle_class)].map(
      (e) => e.id,
    );

    const output = {
      json_fields,
      text_list_fields,
      text_fields,
      check_fields,
      folder_list_fields,
      role_map_fields,
    };

    return output;
  },
  fillTextList: (text_list, element) => {
    // text_list is an array of strings
    // element is an input element
    const val = text_list.join("\r\n");
    element.value = val;
  },
  parseTextList: (element) => {
    // Return the parsed text list
    var out = element.value
      .split("\n")
      .map((e) => e.trim())
      .filter((e) => e);
    return out;
  },
  loadProvider: (page, provider_name) => {
    ApiClient.getPluginConfiguration(ssoConfigurationPage.pluginUniqueId).then(
      (config) => {
        var provider = config.OidConfigs[provider_name] || {};

        const form_elements = ssoConfigurationPage.listArgumentsByType(page);

        page.querySelector("#OidProviderName").value = provider_name;

        form_elements.text_fields.forEach((id) => {
          const el = page.querySelector("#" + id);
          if (el) el.value = provider[id] != null ? String(provider[id]) : "";
        });

        form_elements.json_fields.forEach((id) => {
          if (provider[id])
            page.querySelector("#" + id).value = JSON.stringify(provider[id]);
        });

        form_elements.text_list_fields.forEach((id) => {
          if (provider[id])
            ssoConfigurationPage.fillTextList(
              provider[id],
              page.querySelector("#" + id),
            );
        });

        form_elements.folder_list_fields.forEach((id) => {
          if (provider[id]) {
            ssoConfigurationPage.populateEnabledFolders(
              provider[id],
              page.querySelector(`#${id}`),
            );
          }
        });

        form_elements.check_fields.forEach((id) => {
          const el = page.querySelector("#" + id);
          if (el) el.checked = !!provider[id];
        });

        form_elements.role_map_fields.forEach((id) => {
          const elem = page.querySelector(`#${id}`);
          if (provider[id])
            ssoConfigurationPage.populateRoleMappings(provider[id], elem);
        });
      },
    );
  },
  samlFormFieldMap: {
    SamlEndpoint: "SamlEndpoint",
    SamlClientId: "SamlClientId",
    SamlCertificate: "SamlCertificate",
    Enabled: "SamlEnabled",
    EnableAuthorization: "SamlEnableAuthorization",
    EnableAllFolders: "SamlEnableAllFolders",
    Roles: "SamlRoles",
    AdminRoles: "SamlAdminRoles",
    EnableFolderRoles: "SamlEnableFolderRoles",
    EnableLiveTvRoles: "SamlEnableLiveTvRoles",
    LiveTvRoles: "SamlLiveTvRoles",
    LiveTvManagementRoles: "SamlLiveTvManagementRoles",
    EnableLiveTv: "SamlEnableLiveTv",
    EnableLiveTvManagement: "SamlEnableLiveTvManagement",
    DefaultProvider: "SamlDefaultProvider",
    NewPath: "SamlNewPath",
    SchemeOverride: "SamlSchemeOverride",
    PortOverride: "SamlPortOverride",
  },
  loadSamlProvider: (page, provider_name) => {
    ApiClient.getPluginConfiguration(ssoConfigurationPage.pluginUniqueId).then(
      (config) => {
        const provider = (config.SamlConfigs || {})[provider_name] || {};
        const map = ssoConfigurationPage.samlFormFieldMap;

        page.querySelector("#SamlProviderName").value = provider_name;

        Object.keys(map).forEach((configKey) => {
          const formId = map[configKey];
          const el = page.querySelector("#" + formId);
          if (!el) return;
          const val = provider[configKey];
          if (
            configKey === "PortOverride" ||
            configKey === "SchemeOverride" ||
            configKey === "SamlEndpoint" ||
            configKey === "SamlClientId" ||
            configKey === "SamlCertificate" ||
            configKey === "DefaultProvider"
          ) {
            el.value = val != null ? String(val) : "";
          } else if (
            configKey === "Roles" ||
            configKey === "AdminRoles" ||
            configKey === "LiveTvRoles" ||
            configKey === "LiveTvManagementRoles"
          ) {
            ssoConfigurationPage.fillTextList(
              Array.isArray(val) ? val : [],
              el,
            );
          } else if (
            configKey === "Enabled" ||
            configKey === "EnableAuthorization" ||
            configKey === "EnableAllFolders" ||
            configKey === "EnableFolderRoles" ||
            configKey === "EnableLiveTvRoles" ||
            configKey === "EnableLiveTv" ||
            configKey === "EnableLiveTvManagement" ||
            configKey === "NewPath"
          ) {
            el.checked = !!val;
          }
        });

        const foldersEl = page.querySelector("#SamlEnabledFolders");
        if (foldersEl && provider.EnabledFolders) {
          ssoConfigurationPage.populateEnabledFolders(
            provider.EnabledFolders,
            foldersEl,
          );
        }
        const roleMapEl = page.querySelector("#SamlFolderRoleMapping");
        if (roleMapEl && provider.FolderRoleMapping) {
          ssoConfigurationPage.populateRoleMappings(
            provider.FolderRoleMapping,
            roleMapEl,
          );
        } else if (roleMapEl) {
          ssoConfigurationPage.populateRoleMappings([], roleMapEl);
        }
      },
    );
  },
  saveSamlProvider: (page, provider_name) => {
    const name = (provider_name || "").trim();
    if (name === "") {
      if (typeof Dashboard !== "undefined" && Dashboard.alert) {
        Dashboard.alert("Provider name cannot be empty.");
      } else {
        window.alert("Provider name cannot be empty.");
      }
      return Promise.resolve();
    }
    return new Promise((resolve) => {
      ApiClient.getPluginConfiguration(
        ssoConfigurationPage.pluginUniqueId,
      ).then((config) => {
        if (!config.SamlConfigs) config.SamlConfigs = {};
        const current_config = {};
        const map = ssoConfigurationPage.samlFormFieldMap;

        Object.keys(map).forEach((configKey) => {
          const formId = map[configKey];
          const el = page.querySelector("#" + formId);
          if (!el) return;
          if (configKey === "PortOverride") {
            const raw = (el.value || "").trim();
            const num = raw === "" ? null : parseInt(raw, 10);
            current_config[configKey] =
              num !== null && !isNaN(num) ? num : null;
          } else if (
            configKey === "SamlEndpoint" ||
            configKey === "SamlClientId" ||
            configKey === "SamlCertificate" ||
            configKey === "SchemeOverride" ||
            configKey === "DefaultProvider"
          ) {
            current_config[configKey] = (el.value || "").trim() || null;
          } else if (
            configKey === "Roles" ||
            configKey === "AdminRoles" ||
            configKey === "LiveTvRoles" ||
            configKey === "LiveTvManagementRoles"
          ) {
            current_config[configKey] = ssoConfigurationPage.parseTextList(el);
          } else if (
            configKey === "Enabled" ||
            configKey === "EnableAuthorization" ||
            configKey === "EnableAllFolders" ||
            configKey === "EnableFolderRoles" ||
            configKey === "EnableLiveTvRoles" ||
            configKey === "EnableLiveTv" ||
            configKey === "EnableLiveTvManagement" ||
            configKey === "NewPath"
          ) {
            current_config[configKey] = !!el.checked;
          }
        });

        const foldersEl = page.querySelector("#SamlEnabledFolders");
        current_config.EnabledFolders = foldersEl
          ? ssoConfigurationPage.serializeEnabledFolders(foldersEl)
          : [];
        const roleMapEl = page.querySelector("#SamlFolderRoleMapping");
        current_config.FolderRoleMapping = roleMapEl
          ? ssoConfigurationPage.serializeRoleMappings(roleMapEl)
          : [];

        config.SamlConfigs[name] = current_config;
        ApiClient.updatePluginConfiguration(
          ssoConfigurationPage.pluginUniqueId,
          config,
        ).then(function (result) {
          Dashboard.processPluginConfigurationUpdateResult(result);
          ssoConfigurationPage.loadConfiguration(page);
          ssoConfigurationPage.loadSamlProvider(page, name);
          const sel = page.querySelector("#selectSamlProvider");
          if (sel) sel.value = name;
          if (typeof Dashboard !== "undefined" && Dashboard.alert) {
            Dashboard.alert("Settings saved.");
          } else {
            window.alert("Settings saved.");
          }
          resolve();
        });
      });
    });
  },
  deleteSamlProvider: (page, provider_name) => {
    if (
      !window.confirm(
        "Are you sure you want to delete the SAML provider " +
          provider_name +
          "?",
      )
    )
      return;
    return new Promise((resolve) => {
      ApiClient.getPluginConfiguration(
        ssoConfigurationPage.pluginUniqueId,
      ).then((config) => {
        if (
          !config.SamlConfigs ||
          !config.SamlConfigs.hasOwnProperty(provider_name)
        ) {
          resolve();
          return;
        }
        delete config.SamlConfigs[provider_name];
        ApiClient.updatePluginConfiguration(
          ssoConfigurationPage.pluginUniqueId,
          config,
        ).then(function (result) {
          Dashboard.processPluginConfigurationUpdateResult(result);
          ssoConfigurationPage.loadConfiguration(page);
          if (typeof Dashboard !== "undefined" && Dashboard.alert) {
            Dashboard.alert("Provider removed");
          } else {
            window.alert("Provider removed");
          }
          resolve();
        });
      });
    });
  },
  deleteProvider: (page, provider_name) => {
    if (
      !window.confirm(
        `Are you sure you want to delete the provider ${provider_name}?`,
      )
    ) {
      return;
    }
    return new Promise((resolve) => {
      ApiClient.getPluginConfiguration(
        ssoConfigurationPage.pluginUniqueId,
      ).then((config) => {
        if (!config.OidConfigs.hasOwnProperty(provider_name)) {
          resolve();
          return;
        }

        delete config.OidConfigs[provider_name];
        ApiClient.updatePluginConfiguration(
          ssoConfigurationPage.pluginUniqueId,
          config,
        ).then(function (result) {
          Dashboard.processPluginConfigurationUpdateResult(result);
          ssoConfigurationPage.loadConfiguration(page);

          if (typeof Dashboard !== "undefined" && Dashboard.alert) {
            Dashboard.alert("Provider removed");
          } else {
            window.alert("Provider removed");
          }

          resolve();
        });
      });
    });
  },
  saveProvider: (page, provider_name) => {
    return new Promise((resolve) => {
      provider_name = (provider_name || "").trim();
      if (provider_name === "") {
        if (typeof Dashboard !== "undefined" && Dashboard.alert) {
          Dashboard.alert("Provider name cannot be blank.");
        } else {
          window.alert("Provider name cannot be blank.");
        }
        resolve();
        return;
      }
      const form_elements = ssoConfigurationPage.listArgumentsByType(page);

      ApiClient.getPluginConfiguration(
        ssoConfigurationPage.pluginUniqueId,
      ).then((config) => {
        var current_config = {};
        if (config.OidConfigs.hasOwnProperty(provider_name)) {
          current_config = config.OidConfigs[provider_name];
        }

        form_elements.text_fields.forEach((id) => {
          const el = page.querySelector("#" + id);
          const value = el ? el.value : "";
          if (id === "PortOverride") {
            const num = value.trim() === "" ? null : parseInt(value, 10);
            current_config[id] = num !== null && !isNaN(num) ? num : null;
          } else if (value) {
            current_config[id] = value;
          } else {
            current_config[id] = null;
          }
        });

        form_elements.json_fields.forEach((id) => {
          const value = page.querySelector("#" + id).value;
          if (value) {
            current_config[id] = JSON.parse(value);
          } else {
            current_config[id] = null;
          }
        });

        form_elements.check_fields.forEach((id) => {
          current_config[id] = page.querySelector("#" + id).checked;
        });

        form_elements.text_list_fields.forEach((id) => {
          current_config[id] = ssoConfigurationPage.parseTextList(
            page.querySelector("#" + id),
          );
        });

        form_elements.folder_list_fields.forEach((id) => {
          const elem = page.querySelector(`#${id}`);
          current_config[id] =
            ssoConfigurationPage.serializeEnabledFolders(elem);
        });

        form_elements.role_map_fields.forEach((id) => {
          const elem = page.querySelector(`#${id}`);
          current_config[id] = ssoConfigurationPage.serializeRoleMappings(elem);
        });

        config.OidConfigs[provider_name] = current_config;

        ApiClient.updatePluginConfiguration(
          ssoConfigurationPage.pluginUniqueId,
          config,
        ).then(function (result) {
          Dashboard.processPluginConfigurationUpdateResult(result);
          ssoConfigurationPage.loadConfiguration(page);
          ssoConfigurationPage.loadProvider(page, provider_name);

          page.querySelector("#selectProvider").value = provider_name;
          if (typeof Dashboard !== "undefined" && Dashboard.alert) {
            Dashboard.alert("Settings saved.");
          } else {
            window.alert("Settings saved.");
          }
          resolve();
        });
      });
    });
  },
  addTextAreaStyle: (view) => {
    var style = document.createElement("link");
    style.rel = "stylesheet";
    style.href =
      ApiClient.getUrl("web/configurationpage") + "?name=SSO-Auth.css";
    view.appendChild(style);
  },
};

export default function (view) {
  ssoConfigurationPage.addTextAreaStyle(view);
  ssoConfigurationPage.loadConfiguration(view);

  ssoConfigurationPage.listArgumentsByType(view);

  view.querySelector("#SaveProvider").addEventListener("click", (e) => {
    const target_provider = view.querySelector("#OidProviderName").value;

    ssoConfigurationPage.saveProvider(view, target_provider);

    e.preventDefault();
    return false;
  });

  view.querySelector("#LoadProvider").addEventListener("click", (e) => {
    const target_provider = view.querySelector("#selectProvider").value;

    ssoConfigurationPage.loadProvider(view, target_provider);

    e.preventDefault();
    return false;
  });

  view.querySelector("#DeleteProvider").addEventListener("click", (e) => {
    const target_provider = view.querySelector("#selectProvider").value;

    ssoConfigurationPage.deleteProvider(view, target_provider);

    e.preventDefault();
    return false;
  });

  view.querySelector("#AddRoleMapping").addEventListener("click", (e) => {
    const container = view.querySelector("#FolderRoleMapping");
    const current_mappings =
      ssoConfigurationPage.serializeRoleMappings(container);
    current_mappings.push({ Role: "", Folders: [] });
    ssoConfigurationPage.populateRoleMappings(current_mappings, container);
  });

  view.querySelector("#sso-self-service-link").href =
    ApiClient.getUrl("/SSOViews/linking");

  const loadSamlBtn = view.querySelector("#LoadSamlProvider");
  if (loadSamlBtn) {
    loadSamlBtn.addEventListener("click", (e) => {
      const name = view.querySelector("#selectSamlProvider").value;
      ssoConfigurationPage.loadSamlProvider(view, name);
      e.preventDefault();
      return false;
    });
  }
  const deleteSamlBtn = view.querySelector("#DeleteSamlProvider");
  if (deleteSamlBtn) {
    deleteSamlBtn.addEventListener("click", (e) => {
      const name = view.querySelector("#selectSamlProvider").value;
      ssoConfigurationPage.deleteSamlProvider(view, name);
      e.preventDefault();
      return false;
    });
  }
  const saveSamlBtn = view.querySelector("#SaveSamlProvider");
  if (saveSamlBtn) {
    saveSamlBtn.addEventListener("click", (e) => {
      const name = view.querySelector("#SamlProviderName").value;
      ssoConfigurationPage.saveSamlProvider(view, name);
      e.preventDefault();
      return false;
    });
  }
  const addSamlRoleBtn = view.querySelector("#AddSamlRoleMapping");
  if (addSamlRoleBtn) {
    addSamlRoleBtn.addEventListener("click", (e) => {
      const container = view.querySelector("#SamlFolderRoleMapping");
      const current = ssoConfigurationPage.serializeRoleMappings(container);
      current.push({ Role: "", Folders: [] });
      ssoConfigurationPage.populateRoleMappings(current, container);
      e.preventDefault();
      return false;
    });
  }
}
