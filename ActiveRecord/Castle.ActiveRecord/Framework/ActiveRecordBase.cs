// Copyright 2004-2006 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.ActiveRecord
{
	using System;
	using System.Collections;

	using Castle.ActiveRecord.Framework;
	using Castle.ActiveRecord.Queries;
	
	using NHibernate;
	using NHibernate.Expression;

	/// <summary>
	/// Allow custom executions using the NHibernate's ISession.
	/// </summary>
	public delegate object NHibernateDelegate(ISession session, object instance);

	/// <summary>
	/// Base class for all ActiveRecord classes. Implements 
	/// all the functionality to simplify the code on the 
	/// subclasses.
	/// </summary>
	[Serializable]
	public abstract class ActiveRecordBase : ActiveRecordHooksBase
	{
		/// <summary>
		/// The global holder for the session factories.
		/// </summary>
		protected internal static ISessionFactoryHolder holder;

		/// <summary>
		/// Constructs an ActiveRecordBase subclass.
		/// </summary>
		public ActiveRecordBase()
		{
		}

		#region internal static

		internal static void EnsureInitialized(Type type)
		{
			if (holder == null)
			{
				String message = String.Format("An ActiveRecord class ({0}) was used but the framework seems not " +
				                               "properly initialized. Did you forget about ActiveRecordStarter.Initialize() ?",
				                               type.FullName);
				throw new ActiveRecordException(message);
			}
			if (type != typeof(ActiveRecordBase) && GetModel(type) == null)
			{
				String message = String.Format("You have accessed an ActiveRecord class that wasn't properly initialized. " +
				                               "The only explanation is that the call to ActiveRecordStarter.Initialize() didn't include {0} class",
				                               type.FullName);
				throw new ActiveRecordException(message);
			}
		}

		/// <summary>
		/// Internally used
		/// </summary>
		/// <param name="arType"></param>
		/// <param name="model"></param>
		internal static void Register(Type arType, Framework.Internal.ActiveRecordModel model)
		{
			Framework.Internal.ActiveRecordModel.Register(arType, model);
		}

		/// <summary>
		/// Internally used
		/// </summary>
		/// <param name="arType"></param>
		/// <returns></returns>
		internal static Framework.Internal.ActiveRecordModel GetModel(Type arType)
		{
			return Framework.Internal.ActiveRecordModel.GetModel(arType);
		}

		#endregion

		#region protected internal static

		#region Create/Update/Save/Delete/DeleteAll/Refresh

		#region Create

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be created on the database</param>
		protected internal static void Create(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			EnsureInitialized(instance.GetType());

			ISession session = holder.CreateSession(instance.GetType());

			try
			{
				session.Save(instance);

				session.Flush();
			}
			catch(Exception ex)
			{
				// NHibernate catches our ValidationException, and as such it is the innerexception here
				if (ex.InnerException is ValidationException)
				{
					throw ex.InnerException;
				}
				else
				{
					throw new ActiveRecordException("Could not perform Create for " + instance.GetType().Name, ex);
				}
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region Delete

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be deleted</param>
		protected internal static void Delete(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			EnsureInitialized(instance.GetType());

			ISession session = holder.CreateSession(instance.GetType());

			try
			{
				session.Delete(instance);

				session.Flush();
			}
			catch(Exception ex)
			{
				// NHibernate catches our ValidationException, and as such it is the innerexception here
				if (ex.InnerException is ValidationException)
				{
					throw ex.InnerException;
				}
				else
				{
					throw new ActiveRecordException("Could not perform Delete for " + instance.GetType().Name, ex);
				}
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region Refresh

		/// <summary>
		/// Refresh the instance from the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be reloaded</param>
		protected internal static void Refresh(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			EnsureInitialized(instance.GetType());

			ISession session = holder.CreateSession(instance.GetType());

			try
			{
				session.Refresh(instance);
			}
			catch(Exception ex)
			{
				// NHibernate catches our ValidationException, and as such it is the innerexception here
				if (ex.InnerException is ValidationException)
				{
					throw ex.InnerException;
				}
				else
				{
					throw new ActiveRecordException("Could not perform Refresh for " + instance.GetType().Name, ex);
				}
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region DeleteAll

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		/// <param name="type">ActiveRecord type on which the rows on the database should be deleted</param>
		protected internal static void DeleteAll(Type type)
		{
			EnsureInitialized(type);

			ISession session = holder.CreateSession(type);

			try
			{
				session.Delete(String.Format("from {0}", type.Name));

				session.Flush();
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform DeleteAll for " + type.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Deletes all rows for the specified ActiveRecord type that matches
		/// the supplied HQL condition
		/// </summary>
		/// <remarks>
		/// This method is usually useful for test cases.
		/// </remarks>
		/// <param name="type">ActiveRecord type on which the rows on the database should be deleted</param>
		/// <param name="where">HQL condition to select the rows to be deleted</param>
		protected internal static void DeleteAll(Type type, String where)
		{
			EnsureInitialized(type);

			ISession session = holder.CreateSession(type);

			try
			{
				session.Delete(String.Format("from {0} where {1}", type.Name, where));

				session.Flush();
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform DeleteAll for " + type.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Deletes all <paramref name="targetType" /> objects, based on the primary keys
		/// supplied on <paramref name="pkValues" />.
		/// </summary>
		/// <param name="targetType">The target ActiveRecord type</param>
		/// <param name="pkValues">A list of primary keys</param>
		/// <returns>The number of objects deleted</returns>
		protected internal static int DeleteAll(Type targetType, IEnumerable pkValues)
		{
			if (pkValues == null)
			{
				return 0;
			}

			int counter = 0;

			foreach(object pk in pkValues)
			{
				Object obj = FindByPrimaryKey(targetType, pk, false);

				if (obj != null)
				{
					ActiveRecordBase arBase = obj as ActiveRecordBase;

					if (arBase != null)
					{
						arBase.Delete(); // in order to allow override of the virtual "Delete()" method
					}
					else
					{
						ActiveRecordBase.Delete(obj);
					}

					counter++;
				}
			}

			return counter;
		}

		#endregion

		#region Update

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be updated on the database</param>
		protected internal static void Update(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			EnsureInitialized(instance.GetType());

			ISession session = holder.CreateSession(instance.GetType());

			try
			{
				session.Update(instance);

				session.Flush();
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform Update for " + instance.GetType().Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region Save

		/// <summary>
		/// Saves the instance to the database. If the primary key is unitialized
		/// it creates the instance on the database. Otherwise it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create()"/>
		/// or <see cref="Update()"/> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The ActiveRecord instance to be saved</param>
		protected internal static void Save(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			EnsureInitialized(instance.GetType());

			ISession session = holder.CreateSession(instance.GetType());

			try
			{
				session.SaveOrUpdate(instance);

				session.Flush();
			}
			catch(Exception ex)
			{
				// NHibernate catches our ValidationException, and as such it is the innerexception here
				if (ex.InnerException is ValidationException)
				{
					throw ex.InnerException;
				}
				else
				{
					throw new ActiveRecordException("Could not perform Save for " + instance.GetType().Name, ex);
				}
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#endregion

		#region Execute

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="targetType">The target ActiveRecordType</param>
		/// <param name="call">The delegate instance</param>
		/// <param name="instance">The ActiveRecord instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		protected internal static object Execute(Type targetType, NHibernateDelegate call, object instance)
		{
			if (targetType == null) throw new ArgumentNullException("targetType", "Target type must be informed");
			if (call == null) throw new ArgumentNullException("call", "Delegate must be passed");

			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				return call(session, instance);
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Error performing Execute for " + targetType.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region ExecuteQuery

		/// <summary>
		/// Enumerates the query
		/// Note: only use if you expect most of the values to exist on the second level cache.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns></returns>
		protected internal static IEnumerable EnumerateQuery(IActiveRecordQuery query)
		{
			Type targetType = query.Target;

			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				return query.Enumerate(session);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform EnumerateQuery for " + targetType.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region ExecuteQuery

		/// <summary>
		/// Executes the query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns></returns>
		public static object ExecuteQuery(IActiveRecordQuery query)
		{
			Type targetType = query.Target;

			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				return query.Execute(session);
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform ExecuteQuery for " + targetType.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region Count

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <example>
		/// <code>
		/// [ActiveRecord]
		/// public class User : ActiveRecordBase
		/// {
		///   ...
		///   
		///   public static int CountAllUsers()
		///   {
		///     return CountAll(typeof(User));
		///   }
		/// }
		/// </code>
		/// </example>
		/// <param name="targetType">Type of the target.</param>
		/// <returns>The count result</returns>
		protected internal static int CountAll(Type targetType)
		{
			CountQuery query = new CountQuery(targetType);

			return (int) ExecuteQuery(query);
		}

		/// <summary>
		/// Returns the number of records of the specified 
		/// type in the database
		/// </summary>
		/// <example>
		/// <code>
		/// [ActiveRecord]
		/// public class User : ActiveRecordBase
		/// {
		///   ...
		///   
		///   public static int CountAllUsersLocked()
		///   {
		///     return CountAll(typeof(User), "IsLocked = ?", true);
		///   }
		/// }
		/// </code>
		/// </example>
		/// <param name="targetType">Type of the target.</param>
		/// <param name="filter">A sql where string i.e. Person=? and DOB &gt; ?</param>
		/// <param name="args">Positional parameters for the filter string</param>
		/// <returns>The count result</returns>
		protected internal static int CountAll(Type targetType, string filter, params object[] args)
		{
			CountQuery query = new CountQuery(targetType, filter, args);

			return (int) ExecuteQuery(query);
		}

		#endregion

		#region Exists

		/// <summary>
		/// Check if there is any records in the db for the target type
		/// </summary>
		/// <param name="targetType">Type of the target.</param>
		/// <returns><c>true</c> if there's at least one row</returns>
		protected internal static bool Exists(Type targetType)
		{
			return CountAll(targetType) != 0;
		}

		/// <summary>
		/// Check if there is any records in the db for the target type
		/// </summary>
		/// <param name="targetType">Type of the target.</param>
		/// <param name="filter">A sql where string i.e. Person=? and DOB &gt; ?</param>
		/// <param name="args">Positional parameters for the filter string</param>
		/// <returns><c>true</c> if there's at least one row</returns>
		protected internal static bool Exists(Type targetType, string filter, params object[] args)
		{
			return CountAll(targetType, filter, args) != 0;
		}

		/// <summary>
		/// Check if the <paramref name="id"/> exists in the database.
		/// </summary>
		/// <param name="targetType">Type of the target.</param>
		/// <param name="id">The id to check on</param>
		/// <returns><c>true</c> if the ID exists; otherwise <c>false</c>.</returns>
		protected internal static bool Exists(Type targetType, object id)
		{
			return Exists(targetType, "id=?", id);
		}

		#endregion

		#region FindAll

		/// <summary>
		/// Returns all instances found for the specified type.
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>
		protected internal static Array FindAll(Type targetType)
		{
			return FindAll(targetType, (Order[]) null);
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using sort orders and criterias.
		/// </summary>
		/// <param name="targetType"></param>
		/// <param name="orders"></param>
		/// <param name="criterias"></param>
		/// <returns></returns>
		protected internal static Array FindAll(Type targetType, Order[] orders, params ICriterion[] criterias)
		{
			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				ICriteria criteria = session.CreateCriteria(targetType);

				foreach(ICriterion cond in criterias)
				{
					criteria.Add(cond);
				}

				if (orders != null)
				{
					foreach(Order order in orders)
					{
						criteria.AddOrder(order);
					}
				}

				return SupportingUtils.BuildArray(targetType, criteria.List());
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform FindAll for " + targetType.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Returns all instances found for the specified type 
		/// using criterias.
		/// </summary>
		/// <param name="targetType"></param>
		/// <param name="criterias"></param>
		/// <returns></returns>
		protected internal static Array FindAll(Type targetType, params ICriterion[] criterias)
		{
			return FindAll(targetType, null, criterias);
		}

		#endregion

		#region FindAllByProperty

		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="targetType">The target type</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		protected internal static Array FindAllByProperty(Type targetType, String property, object value)
		{
			ICriterion criteria = (value == null) ? Expression.IsNull(property) : Expression.Eq(property, value);
			return FindAll(targetType, criteria);
		}

		/// <summary>
		/// Finds records based on a property value - automatically converts null values to IS NULL style queries. 
		/// </summary>
		/// <param name="targetType">The target type</param>
		/// <param name="orderByColumn">The column name to be ordered ASC</param>
		/// <param name="property">A property name (not a column name)</param>
		/// <param name="value">The value to be equals to</param>
		/// <returns></returns>
		protected internal static Array FindAllByProperty(Type targetType, String orderByColumn, String property, object value)
		{
			ICriterion criteria = (value == null) ? Expression.IsNull(property) : Expression.Eq(property, value);
			return FindAll(targetType, new Order[] { Order.Asc(orderByColumn) }, criteria);
		}

		#endregion

		#region FindByPrimaryKey

		/// <summary>
		/// Finds an object instance by an unique ID
		/// </summary>
		/// <param name="targetType">The AR subclass type</param>
		/// <param name="id">ID value</param>
		/// <returns></returns>
		protected internal static object FindByPrimaryKey(Type targetType, object id)
		{
			return FindByPrimaryKey(targetType, id, true);
		}

		/// <summary>
		/// Finds an object instance by an unique ID
		/// </summary>
		/// <param name="targetType">The AR subclass type</param>
		/// <param name="id">ID value</param>
		/// <param name="throwOnNotFound"><c>true</c> if you want to catch an exception 
		/// if the object is not found</param>
		/// <returns></returns>
		/// <exception cref="ObjectNotFoundException">if <c>throwOnNotFound</c> is set to 
		/// <c>true</c> and the row is not found</exception>
		protected internal static object FindByPrimaryKey(Type targetType, object id, bool throwOnNotFound)
		{
			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				return session.Load(targetType, id);
			}
			catch(ObjectNotFoundException ex)
			{
				if (throwOnNotFound)
				{
					String message = String.Format("Could not find {0} with id {1}", targetType.Name, id);
					throw new NotFoundException(message, ex);
				}

				return null;
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform FindByPrimaryKey for " + targetType.Name + ". Id: " + id, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		#endregion

		#region FindFirst

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="targetType">The target type</param>
		/// <param name="orders">The sort order - used to determine which record is the first one</param>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		protected internal static object FindFirst(Type targetType, Order[] orders, params ICriterion[] criterias)
		{
			Array result = SlicedFindAll(targetType, 0, 1, orders, criterias);
			return (result != null && result.Length > 0 ? result.GetValue(0) : null);
		}

		/// <summary>
		/// Searches and returns the first row.
		/// </summary>
		/// <param name="targetType">The target type</param>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		protected internal static object FindFirst(Type targetType, params ICriterion[] criterias)
		{
			return FindFirst(targetType, null, criterias);
		}

		#endregion

		#region FindOne

		/// <summary>
		/// Searches and returns a row. If more than one is found, 
		/// throws <see cref="ActiveRecordException"/>
		/// </summary>
		/// <param name="targetType">The target type</param>
		/// <param name="criterias">The criteria expression</param>
		/// <returns>A <c>targetType</c> instance or <c>null</c></returns>
		protected internal static object FindOne(Type targetType, params ICriterion[] criterias)
		{
			Array result = SlicedFindAll(targetType, 0, 2, criterias);

			if (result.Length > 1)
			{
				throw new ActiveRecordException(targetType.Name + ".FindOne returned " + result.Length +
				                                " rows. Expecting one or none");
			}

			return (result.Length == 0) ? null : result.GetValue(0);
		}

		#endregion

		#region SlicedFindAll

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		protected internal static Array SlicedFindAll(Type targetType, int firstResult, int maxResults, 
		                                              Order[] orders, params ICriterion[] criterias)
		{
			EnsureInitialized(targetType);

			ISession session = holder.CreateSession(targetType);

			try
			{
				ICriteria criteria = session.CreateCriteria(targetType);

				foreach(ICriterion cond in criterias)
				{
					criteria.Add(cond);
				}

				if (orders != null)
				{
					foreach(Order order in orders)
					{
						criteria.AddOrder(order);
					}
				}

				criteria.SetFirstResult(firstResult);
				criteria.SetMaxResults(maxResults);

				return SupportingUtils.BuildArray(targetType, criteria.List());
			}
			catch(ValidationException)
			{
				throw;
			}
			catch(Exception ex)
			{
				throw new ActiveRecordException("Could not perform SlicedFindAll for " + targetType.Name, ex);
			}
			finally
			{
				holder.ReleaseSession(session);
			}
		}

		/// <summary>
		/// Returns a portion of the query results (sliced)
		/// </summary>
		protected internal static Array SlicedFindAll(Type targetType, int firstResult, int maxResults,
		                                              params ICriterion[] criterias)
		{
			return SlicedFindAll(targetType, firstResult, maxResults, null, criterias);
		}

		#endregion

		#endregion

		#region protected internal

		/// <summary>
		/// Invokes the specified delegate passing a valid 
		/// NHibernate session. Used for custom NHibernate queries.
		/// </summary>
		/// <param name="call">The delegate instance</param>
		/// <returns>Whatever is returned by the delegate invocation</returns>
		protected internal object Execute(NHibernateDelegate call)
		{
			return Execute(GetType(), call, this);
		}

		#endregion

		#region public virtual

		/// <summary>
		/// Saves the instance information to the database.
		/// May Create or Update the instance depending 
		/// on whether it has a valid ID.
		/// </summary>
		public virtual void Save()
		{
			ActiveRecordBase.Save(this);
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		public virtual void Create()
		{
			ActiveRecordBase.Create(this);
		}

		/// <summary>
		/// Persists the modification on the instance
		/// state to the database.
		/// </summary>
		public virtual void Update()
		{
			ActiveRecordBase.Update(this);
		}

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		public virtual void Delete()
		{
			ActiveRecordBase.Delete(this);
		}

		/// <summary>
		/// Refresh the instance from the database.
		/// </summary>
		public virtual void Refresh()
		{
			ActiveRecordBase.Refresh(this);
		}

		#endregion

		#region public override

		/// <summary>
		/// Return the type of the object with its PK value.
		/// Useful for logging/debugging
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			Framework.Internal.ActiveRecordModel model = GetModel(GetType());

			if (model == null || model.PrimaryKey == null)
			{
				return base.ToString();
			}

			Framework.Internal.PrimaryKeyModel pkModel = model.PrimaryKey;

			object pkVal = pkModel.Property.GetValue(this, null);

			return base.ToString() + "#" + pkVal;
		}

		#endregion
	}
}
